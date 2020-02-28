using System;
using System.IO;
using System.Linq;
using AElf.CSharp.CodeOps;

namespace AElf.Contracts.Deployer
{
    class Program
    {
        static int Main(string[] args)
        {
            var skipAudit = false;
            var overwrite = false;
            var sourcePath = args.Last();
            string saveAsPath;

            // We may later use a proper parser to get arguments if we need to support more
            foreach (var arg in args)
            {
                // Set flags
                if (arg.StartsWith("-"))
                {
                    switch (arg)
                    {
                        case "-overwrite":
                            overwrite = true;
                            break;
                        
                        case "-skipaudit":
                            skipAudit = true;
                            break;
                    }
                }
            }

            if (overwrite)
            {
                saveAsPath = sourcePath;
                Console.WriteLine($"[CONTRACT-PATCHER] Overwriting {saveAsPath}");
            }
            else
            {
                saveAsPath = sourcePath + ".patched";
                Console.WriteLine($"[CONTRACT-PATCHER] Saving as {saveAsPath}");
            }
            
            var patchedCode = ContractPatcher.Patch(File.ReadAllBytes(sourcePath));

            if (!skipAudit)
            {
                try
                {
                    var auditor = new ContractAuditor(null, null);
                    auditor.Audit(patchedCode, null, false);
                }
                catch (InvalidCodeException ex)
                {
                    foreach (var finding in ex.Findings)
                    {
                        // Print error in parsable format so that it can be shown in IDE
                        Console.WriteLine($"error: {finding.ToString()}");
                    }
                    return 1;
                }                
            }

            File.WriteAllBytes(saveAsPath, patchedCode);

            return 0;
        }
    }
}
