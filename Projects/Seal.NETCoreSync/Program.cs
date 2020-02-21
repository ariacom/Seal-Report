using System.IO;
using System.Reflection;

namespace Seal.NETCoreSync
{
    class Program
    {
        static void Main(string[] args)
        {
            var rootDir = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), @"..\..\..\..\");
            Sync.ToNETCore(rootDir);
        }
    }
}
