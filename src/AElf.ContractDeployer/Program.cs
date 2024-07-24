using System;
using System.Collections.Generic;
using System.IO;
using AElf.CSharp.CodeOps;
using AElf.Kernel.CodeCheck.Infrastructure;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;

namespace AElf.ContractDeployer;

internal class Program
{
    private static void Main(string[] args)
    {
        Parser.Default.ParseArguments<Options>(args)
            .WithParsed(Run)
            .WithNotParsed(Error);
    }

    private static void Error(IEnumerable<Error> errors)
    {
        Console.WriteLine("error: Problem parsing input parameters to run contract deployer.");
    }

    private static void Run(Options o)
    {
        string saveAsPath;

        if (!File.Exists(o.ContractDllPath))
        {
            Console.WriteLine($"error: Contract DLL cannot be found in specified path {o.ContractDllPath}");
            return;
        }

        if (o.Overwrite)
        {
            saveAsPath = o.ContractDllPath;
            Console.WriteLine($"[CONTRACT-PATCHER] Overwriting {saveAsPath}");
        }
        else
        {
            saveAsPath = o.ContractDllPath + ".patched";
            Console.WriteLine($"[CONTRACT-PATCHER] Saving as {saveAsPath}");
        }
        
        using var application = AbpApplicationFactory.Create<ContractDeployerModule>();
        application.Initialize();
        var contractPatcher = application.ServiceProvider.GetRequiredService<IContractPatcher>();
        var patchedCode = contractPatcher.Patch(File.ReadAllBytes(o.ContractDllPath), o.IsSystemContract);

        if (!o.SkipAudit)
            try
            {
                var auditor = application.ServiceProvider.GetRequiredService<IContractAuditor>();
                auditor.Audit(patchedCode, null, o.IsSystemContract);
            }
            catch (CSharpCodeCheckException ex)
            {
                foreach (var finding in ex.Findings)
                    // Print error in parsable format so that it can be shown in IDE
                    Console.WriteLine($"error: {finding}");
            }
            catch (Exception e)
            {
                Console.WriteLine("Audit exception: ",e);
            }

        File.WriteAllBytes(saveAsPath, patchedCode);
    }

    private class Options
    {
        [Option('s', "skipaudit", Default = false, HelpText = "Skip performing code check on contract code.")]
        public bool SkipAudit { get; set; }

        [Option('w', "overwrite", Default = false,
            HelpText = "Overwrite contract's DLL instead of saving with .patched extension.")]
        public bool Overwrite { get; set; }

        [Option('t', "treatsystem", Default = false, HelpText = "Treat as system contract when patching.")]
        public bool IsSystemContract { get; set; }

        [Option('p', "path", Required = true, HelpText = "The path of the contract's DLL.")]
        public string ContractDllPath { get; set; }
    }
}