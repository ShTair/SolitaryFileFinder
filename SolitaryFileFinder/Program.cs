using SolitaryFileFinder.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SolitaryFileFinder
{
    class Program
    {
        private static Regex _ref1 = new Regex(@"(?<=href="").+?(?="")");
        private static Regex _ref2 = new Regex(@"(?<=src="").+?(?="")");

        static void Main(string[] args)
        {
            if (args.Length == 0) args = new string[] { null };
            Run(args[0]).Wait();
            Console.WriteLine("** Finished");
            Console.ReadLine();
        }

        private static Queue<Uri> _checkQueue = new Queue<Uri>();
        private static HashSet<string> _findPath = new HashSet<string>();

        private static async Task Run(string pp)
        {
            var p = await SiteParameters.LoadAsync(pp);
            if (p == null)
            {
                p = SiteParameters.GenerateInitial();

                if (pp == null) pp = "param.json";
                await SiteParameters.SaveAsync(pp, p);
                return;
            }

            var baseUri = new Uri("http://temp/");

            foreach (var root in p.RootFiles)
            {
                var rootUri = new Uri(baseUri, root);
                _findPath.Add(rootUri.LocalPath);
                _checkQueue.Enqueue(rootUri);
            }

            var checkPatterns = p.CheckPatterns.Select(t => new Regex(t));

            while (true)
            {
                if (_checkQueue.Count == 0) break;
                var targetUri = _checkQueue.Dequeue();

                IEnumerable<string> refPathes = null;
                if (targetUri.LocalPath[targetUri.LocalPath.Length - 1] != '/')
                {
                    var tp = targetUri.ToPath(p.Path);
                    if (!checkPatterns.Any(t => t.Match(Path.GetFileName(tp)).Success)) continue;

                    try { refPathes = await LoadRefPathes(tp); }
                    catch { continue; }
                }
                else
                {
                    foreach (var defaultName in p.DefaultNames)
                    {
                        var nextUri = new Uri(targetUri, defaultName);
                        var nextPath = nextUri.ToPath(p.Path);
                        if (File.Exists(nextPath))
                        {
                            refPathes = await LoadRefPathes(nextPath);
                            break;
                        }
                    }
                    if (refPathes == null) refPathes = Enumerable.Empty<string>();
                }

                foreach (var su in refPathes.Select(t => new Uri(targetUri, t)).Where(t => t.Host == "temp"))
                {
                    var lp = su.LocalPath;
                    if (_findPath.Add(lp))
                    {
                        _checkQueue.Enqueue(su);
                    }
                }
            }
        }

        private static async Task<IEnumerable<string>> LoadRefPathes(string path)
        {
            using (var reader = File.OpenText(path))
            {
                var data = await reader.ReadToEndAsync();
                return FindRefPathes(data);
            }
        }

        private static IEnumerable<string> FindRefPathes(string data)
        {
            var ms = _ref1.Matches(data);
            foreach (Match m in ms) yield return m.Value;
        }
    }

    static class Extensions
    {
        public static string ToPath(this Uri uri, string dir)
        {
            var a = uri.LocalPath;
            return dir + a.Replace('/', '\\');
        }
    }
}
