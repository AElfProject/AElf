using System;
using System.Collections.Generic;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.CodeCheck.Infrastructure;
using AElf.Kernel.SmartContract;
using Google.Protobuf;

namespace AElf.Kernel.CodeCheck.Application;

public class CodePatchService : ICodePatchService, ITransientDependency
{
    private readonly IContractPatcherContainer _contractPatcherContainer;

    public CodePatchService(IContractPatcherContainer contractPatcherContainer)
    {
        _contractPatcherContainer = contractPatcherContainer;
    }

    public ILogger<CodeCheckService> Logger { get; set; }

    public bool PerformCodePatch(byte[] code, int category, bool isSystemContract, out byte[] patchedCode)
    {
        try
        {
            Logger.LogTrace("Start code patch");
            if (!_contractPatcherContainer.TryGetContractPatcher(category, out var contractPatcher))
            {
                throw new Exception($"Unrecognized contract category: {category}");
            }

            patchedCode = contractPatcher.Patch(code, isSystemContract);
            Logger.LogTrace("Finish code patch");
            return true;
        }
        catch (Exception e)
        {
            Logger.LogWarning(e,$"Perform code patch failed. {e.Message}");
            patchedCode = Array.Empty<byte>();
            return false;
        }
    }
}