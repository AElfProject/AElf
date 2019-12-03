using System;
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
            
            // Overwrite
            File.WriteAllBytes(sourceDllPath, ContractPatcher.Patch(code));
        }
    }
}
