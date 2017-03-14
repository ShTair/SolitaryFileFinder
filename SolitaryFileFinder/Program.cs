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
        private static Regex _ref3 = new Regex(@"(?<=virtual="").+?(?="")");
        private static Regex _ref4 = new Regex(@"(?<=url\().+?(?=\))");

        static void Main(string[] args)
        {
            if (args.Length == 0) args = new string[] { null };
            Run(args[0]).Wait();
            Console.WriteLine("** Finished");
            Console.ReadLine();
        }

        private static Queue<Uri> _checkQueue = new Queue<Uri>();
        private static HashSet<string> _findPath = new HashSet<string>();
        private static HashSet<string> _existsPathes = new HashSet<string>();

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

            FindAll(p.Path, baseUri, p.IgnorePaths);

            foreach (var root in p.RootFiles)
            {
                var rootUri = new Uri(baseUri, root);
                _findPath.Add(rootUri.LocalPath.ToLower());
                _existsPathes.Remove(rootUri.LocalPath.ToLower());
                _checkQueue.Enqueue(rootUri);
            }

            var checkPatterns = p.CheckPatterns.Select(t => new Regex(t)).ToArray();

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

                foreach (var s in refPathes)
                {
                    if (s.StartsWith("mailto:")) continue;

                    var su = new Uri(targetUri, s);
                    if (su.Host != "temp") continue;

                    var lp = su.LocalPath.ToLower();
                    if (_findPath.Add(lp))
                    {
                        _existsPathes.Remove(lp);
                        _checkQueue.Enqueue(su);
                    }
                }
            }

            using (var writer = File.CreateText("out.txt"))
            {
                var ips = p.IgnorePatterns.Select(t => new Regex(t)).ToArray();

                foreach (var item in _existsPathes)
                {
                    if (ips.Any(t => t.Match(item).Success)) continue;
                    await writer.WriteLineAsync(item);
                }
            }
        }

        private static void FindAll(string dir, Uri uri, List<string> ignores)
        {
            foreach (var path in Directory.EnumerateFiles(dir))
            {
                var name = Path.GetFileName(path);
                var cu = new Uri(uri, name);

                var lp = cu.LocalPath.ToLower();

                if (ignores.Contains(lp)) continue;

                _existsPathes.Add(lp);
            }

            foreach (var path in Directory.EnumerateDirectories(dir))
            {
                var name = Path.GetFileName(path);
                var cu = new Uri(uri, name + "/");

                var lp = cu.LocalPath.ToLower();

                if (ignores.Contains(lp)) continue;

                FindAll(path, cu, ignores);
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
            foreach (Match m in _ref1.Matches(data)) yield return m.Value;

            foreach (Match m in _ref2.Matches(data)) yield return m.Value;

            foreach (Match m in _ref3.Matches(data)) yield return m.Value;
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
