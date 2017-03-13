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
        private static Regex _h1 = new Regex(@"(?<=href="").+?(?="")");

        static void Main(string[] args)
        {
            if (args.Length == 0) args = new string[] { null };
            Run(args[0]).Wait();
            Console.WriteLine("** Finished");
            Console.ReadLine();
        }

        private static Queue<Uri> _q = new Queue<Uri>();
        private static HashSet<string> _lp = new HashSet<string>();

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

            var b = new Uri("http://temp/");
            var u = new Uri(b, "index.html");

            _lp.Add(u.LocalPath);
            _q.Enqueue(u);

            var cpt = p.CheckPatterns.Select(t => new Regex(t));

            while (true)
            {
                if (_q.Count == 0) break;
                var tgu = _q.Dequeue();
                IEnumerable<string> s = null;
                if (tgu.LocalPath[tgu.LocalPath.Length - 1] != '/')
                {
                    var tp = tgu.ToPath(p.Path);
                    if (!cpt.Any(t => t.Match(Path.GetFileName(tp)).Success))
                    {
                        continue;
                    }
                    try
                    {
                        s = await Get(tp);
                    }
                    catch
                    {
                        continue;
                    }
                }
                else
                {
                    foreach (var ai in p.DefaultNames)
                    {
                        var r = new Uri(tgu, ai);
                        var ppo = r.ToPath(p.Path);
                        if (File.Exists(ppo))
                        {
                            s = await Get(ppo);
                            break;
                        }
                    }
                    if (s == null) s = Enumerable.Empty<string>();
                }

                foreach (var su in s.Select(t => new Uri(tgu, t)))
                {
                    if (su.Host != "temp") continue;

                    var lp = su.LocalPath;
                    if (_lp.Add(lp))
                    {
                        _q.Enqueue(su);
                    }
                }
            }
        }

        private static async Task<IEnumerable<string>> Get(string path)
        {
            using (var a = File.OpenText(path))
            {
                var str = await a.ReadToEndAsync();
                return PS(str);
            }
        }

        private static IEnumerable<string> PS(string data)
        {
            var ms = _h1.Matches(data);
            foreach (Match m in ms) yield return m.Value;
        }
    }

    static class Extensions
    {
        public static string ToPath(this Uri uri, string b)
        {
            var a = uri.LocalPath;
            return b + a.Replace('/', '\\');
        }
    }
}
