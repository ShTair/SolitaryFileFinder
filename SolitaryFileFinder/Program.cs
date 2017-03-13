using SolitaryFileFinder.Models;
using System;
using System.Threading.Tasks;

namespace SolitaryFileFinder
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0) args = new string[] { null };
            Run(args[0]).Wait();
            Console.WriteLine("** Finished");
            Console.ReadLine();
        }

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
        }
    }
}
