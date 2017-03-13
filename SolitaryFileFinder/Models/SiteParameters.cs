using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SolitaryFileFinder.Models
{
    class SiteParameters
    {
        public string Path { get; set; }

        public List<string> RootFiles { get; set; }

        public List<string> DefaultNames { get; set; }

        public List<string> IgnorePaths { get; set; }

        public List<string> CheckPatterns { get; set; }

        public List<string> IgnorePatterns { get; set; }

        public static SiteParameters GenerateInitial()
        {
            return new SiteParameters
            {
                Path = "",
                RootFiles = new List<string> { "/index.html" },
                DefaultNames = new List<string> { "index.html", "index.htm" },
                IgnorePaths = new List<string> { "/.git/" },
                CheckPatterns = new List<string> { @".*\.html", @".*\.htm" },
                IgnorePatterns = new List<string> { @"\.htaccess", @"\.htpasswd" },
            };
        }

        public static async Task<SiteParameters> LoadAsync(string path)
        {
            try
            {
                string json;
                using (var reader = File.OpenText(path))
                {
                    json = await reader.ReadToEndAsync();
                }
                return JsonConvert.DeserializeObject<SiteParameters>(json);
            }
            catch
            {
                return null;
            }
        }

        public static async Task SaveAsync(string path, SiteParameters value)
        {
            var json = JsonConvert.SerializeObject(value, Formatting.Indented);
            using (var writer = File.CreateText(path))
            {
                await writer.WriteAsync(json);
            }
        }
    }
}
