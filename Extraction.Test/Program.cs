using Framework.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Extraction.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Requires a directory");
                return;
            }

            string dir = args[0];
            var app = new App();
            app.Start("pgp187-bmanci01", "app", "app");

            
            var task = Task.Run(async () =>
            {
                await ProcessDir(app, dir);
            });
            task.Wait();
        }

        static Semaphore semaphore = new Semaphore(0, 1000);

        static async Task ProcessDir(App app, string dir)
        {
            var dirs = Directory.GetDirectories(dir).OrderBy(p => p).ToArray();
            var dirqueue = new ConcurrentQueue<string>();
            for (var i = 0; i < 100; i++)
            {
                var subdir = dirs[i];
                var files = Directory.EnumerateFiles(subdir);
                foreach (var file in files)
                {
                    dirqueue.Enqueue(file);
                }
            }

            string test;
            dirqueue.TryPeek(out test);
            Console.WriteLine(test);
            Console.WriteLine("Processing: " + dirqueue.Count);
            var sw = Stopwatch.StartNew();
            semaphore = new Semaphore(100, 100);

            var tasks = new List<Task>();

            string queuedFile;
            while (dirqueue.TryDequeue(out queuedFile))
            {
                semaphore.WaitOne();
                tasks.Add(ProcessFile(app, queuedFile));
            }

            await Task.WhenAll(tasks);
            sw.Stop();
            Console.WriteLine("Completed in " + sw.Elapsed);
        }

        static async Task ProcessFile(App app, string inputPath)
        {
            var result = await app.Publish("concepts.extract", inputPath);
            semaphore.Release();
        }
    }
}
