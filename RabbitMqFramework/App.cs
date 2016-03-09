using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMqFramework
{
    public class App
    {
        protected IConnection connection;
        protected IModel channel;
        protected string callbackQueueName;
        protected EventingBasicConsumer callbackConsumer;
        protected Dictionary<string, Action<string>> callbacks;
        protected List<Tuple<string, Func<string, string>>> deferredHandlers;
        protected List<Tuple<string, Func<string, string>>> deferredListeners;

        public App()
        {
            deferredHandlers = new List<Tuple<string, Func<string, string>>>();
            deferredListeners = new List<Tuple<string, Func<string, string>>>();
            callbacks = new Dictionary<string, Action<string>>();
        }

        public void Start(string brokerPath) 
        {
            var factory = new ConnectionFactory { HostName = brokerPath };
            connection = factory.CreateConnection();
            channel = connection.CreateModel();
            Console.WriteLine("Connected to " + brokerPath);

            var callbackQueueResult = channel.QueueDeclare(queue: "", durable: true, exclusive: true, autoDelete: false, arguments: null);
            callbackQueueName = callbackQueueResult.QueueName;

            callbackConsumer = new EventingBasicConsumer(channel);
            callbackConsumer.Received += (chan, msg) =>
            {
                var correlationId = msg.BasicProperties.CorrelationId;
                Console.WriteLine(" [x] completed " + correlationId);

                if (callbacks.ContainsKey(correlationId))
                {
                    var message = Encoding.UTF8.GetString(msg.Body);
                    callbacks[correlationId](message);
                }
            };
            channel.BasicConsume(queue: callbackQueueName, noAck: true, consumer: callbackConsumer);

            foreach(var binding in deferredHandlers)
                Handle(binding.Item1, binding.Item2);

            Console.WriteLine("Service has successfully started");
        }

        public void Handle(string eventName, Func<string, string> processMsg) 
        {
            if (channel == null)
                deferredHandlers.Add(new Tuple<string, Func<string, string>>(eventName, processMsg));
            else
                Handler(eventName, processMsg);
        }

        public async Task<string> Publish(string eventName, string data, string correlationId = null)
        {
            if (correlationId == null)
                correlationId = Guid.NewGuid().ToString();

            Console.WriteLine(string.Format(" [f] publishing {0} {1}", eventName, correlationId));

            channel.ExchangeDeclare("app", "topic", true);

            var buffer = Encoding.UTF8.GetBytes(data);

            var tcs = new TaskCompletionSource<string>();
            var task = tcs.Task;
            callbacks[correlationId] = (string msg) => tcs.SetResult(msg);

            var properties = channel.CreateBasicProperties();
            properties.CorrelationId = correlationId;
            properties.ReplyTo = callbackQueueName;
            channel.BasicPublish("app", eventName, properties, buffer);

            // await completion
            var result = await task;
            return result;
        }


        protected void Handler(string eventName, Func<string, string>  processMsg)
        {
            Console.WriteLine("Handling " + eventName);

            channel.ExchangeDeclare("app", "topic", true);
            channel.QueueDeclare(eventName, true, false, false, null);
            channel.QueueBind(eventName, "app", eventName);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (chan, msg) => HandleMessage(eventName, msg, processMsg);
            channel.BasicConsume(eventName, true, consumer);
        }

        protected void HandleMessage(string eventName, BasicDeliverEventArgs msg, Func<string, string> processMsg)
        {
            var correlationId = msg.BasicProperties.CorrelationId;
            var replyTo = msg.BasicProperties.ReplyTo;

            Console.WriteLine(string.Format(" [f] handling {0} {1}", eventName, correlationId));

            var input = Encoding.UTF8.GetString(msg.Body);
            var result = processMsg(input);
            var buffer = Encoding.UTF8.GetBytes(result);
            var properties = channel.CreateBasicProperties();
            properties.CorrelationId = correlationId;

            if(replyTo != null) 
                channel.BasicPublish("", replyTo, properties, buffer);

            channel.BasicPublish("app", eventName + ".complete", properties, buffer);
        }

        
    }
}
