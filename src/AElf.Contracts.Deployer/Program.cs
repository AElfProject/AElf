using System.IO;
using AElf.CSharp.CodeOps;

namespace AElf.Contracts.Deployer
{
    class Program
    {
        static void Main(string[] args)
        {
            var sourceDllPath = args[0];
            var code = File.ReadAllBytes(sourceDllPath);
            
            // Save as
            File.WriteAllBytes(sourceDllPath + ".patched", ContractPatcher.Patch(code));
        }
    }
}
