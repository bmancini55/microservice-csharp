using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framework.Core;

namespace Framework.Examples
{
    class Program
    {
        static void Main(string[] args)
        {
            var task = Task.Run(async () =>
            {
                await MainAsync(args);
            });
            task.Wait();
        }

        static async Task MainAsync(string[] args)
        {
            string mode = "consume";
            if (args.Length == 1)
                mode = args[0];

            switch(mode)
            {
                case "produce":
                    await Produce();
                    break;
                case "consume":
                    await Consume();
                    break;
            }
        }

        static async Task Produce()
        {
            using (var app = new App())
            {
                app.Start("pgp187-bmanci01", "app", "app");
                for (var i = 0; i < 10; i++)
                    await app.Publish("test", "test");
            }
        }

        static async Task Consume()
        {
            var app = new App();
            app.Handle("test", async (msg) =>
            {
                Console.WriteLine(" [o] received " + msg);
                return "done";
            });
            app.Start("pgp187-bmanci01", "app", "app");
        }
    }
}
