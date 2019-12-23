using System;
using System.IO;
using AElf.CSharp.CodeOps;

namespace AElf.Contracts.Deployer
{
    class Program
    {
        static void Main(string[] args)
        {
            var sourcePath = "";
            var saveAsPath = "";
            
            // We may later use a proper parser to get arguments if we need to support more
            if (args[0] != "-overwrite")
            {
                sourcePath = args[0];
                saveAsPath = sourcePath + ".patched";
                Console.WriteLine($"[SAVING AS] {saveAsPath}");
            }
            else
            {
                sourcePath = args[1];
                saveAsPath = sourcePath;
                Console.WriteLine($"[OVERWRITING] {saveAsPath}");
            }
            
            var code = File.ReadAllBytes(sourcePath);
            File.WriteAllBytes(saveAsPath, ContractPatcher.Patch(code));
        }
    }
}
