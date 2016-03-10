using Framework.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroService.TextAndConcept
{
    public class Service
    {
        App app;
        Extraction.Basis.Runner conceptExtractor;
        Extraction.DtSearch.Runner textExtractor;

        public void Start()
        {
            app = new App();
            app.Start("pgp187-bmanci01", "app", "app");
            app.Handle("concepts.extract", HandleConceptExtract);
        }

        public void Stop()
        {
            app.Stop();
        }

        async Task<string> HandleConceptExtract(string file)
        {
            var text = await HandleTextExtract(file);
            Stopwatch sw = new Stopwatch();
            sw.Start();
            conceptExtractor.Process(text);
            sw.Stop();
            app.Broadcast("log.cex", file + "|HandleConceptExtract|" + sw.ElapsedMilliseconds);

            return "ok";
        }

        async Task<string> HandleTextExtract(string file)
        {
            var fileBytes = await ReadFileText(file);
            Stopwatch sw = new Stopwatch();
            sw.Start();
            var text = textExtractor.Process(fileBytes);
            sw.Stop();
            app.Broadcast("log.cex", file + "|HandleTextExtract|" + sw.ElapsedMilliseconds);

            return text;
        }

        async Task<byte[]> ReadFileText(string file)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            var fileInfo = new FileInfo(file);
            var length = fileInfo.Length;
            byte[] buffer = new byte[length];
            using (var fs = fileInfo.Open(FileMode.Open))
            {
                await fs.ReadAsync(buffer, 0, (int)length);
            }
            sw.Stop();
            app.Broadcast("log.cex", file + "|fileSize|" + length + "|ReadFileText|" + sw.ElapsedMilliseconds);

            return buffer;
        }
    }
}
