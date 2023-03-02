using System;
using System.Collections.Generic;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.CodeCheck.Infrastructure;
using AElf.Kernel.SmartContract;
using Google.Protobuf;

namespace AElf.Kernel.CodeCheck.Application;

public class CodeCheckService : ICodeCheckService, ITransientDependency
{
    private readonly CodeCheckOptions _codeCheckOptions;
    private readonly IContractAuditorContainer _contractAuditorContainer;
    private readonly IRequiredAcsProvider _requiredAcsProvider;
    private readonly IContractPatcherContainer _contractPatcherContainer;
    private readonly ISmartContractCodeService _smartContractCodeService;

    public CodeCheckService(IRequiredAcsProvider requiredAcsProvider,
        IContractAuditorContainer contractAuditorContainer,
        IOptionsMonitor<CodeCheckOptions> codeCheckOptionsMonitor, IContractPatcherContainer contractPatcherContainer,
        ISmartContractCodeService smartContractCodeService)
    {
        _requiredAcsProvider = requiredAcsProvider;
        _contractAuditorContainer = contractAuditorContainer;
        _contractPatcherContainer = contractPatcherContainer;
        _smartContractCodeService = smartContractCodeService;
        _codeCheckOptions = codeCheckOptionsMonitor.CurrentValue;
    }

    public ILogger<CodeCheckService> Logger { get; set; }

    public async Task<bool> PerformCodeCheckAsync(byte[] code, Hash blockHash, long blockHeight, int category,
        bool isSystemContract, bool isUserContract)
    {
        if (!_codeCheckOptions.CodeCheckEnabled)
            return true;

        var requiredAcs = new RequiredAcs
        {
            AcsList = new List<string>(),
            RequireAll = false
        };
        
        if (isUserContract)
        {
            requiredAcs = await _requiredAcsProvider.GetRequiredAcsInContractsAsync(blockHash, blockHeight);
        }
        
        try
        {
            // Check contract code
            Logger.LogTrace("Start code check");
            if (!_contractAuditorContainer.TryGetContractAuditor(category, out var contractAuditor))
            {
                Logger.LogWarning("Unrecognized contract category: {Category}", category);
                return false;
            }

            contractAuditor.Audit(code, requiredAcs, isSystemContract);
            Logger.LogTrace("Finish code check");
            return true;
        }
        catch (InvalidCodeException e)
        {
            // May do something else to indicate that the contract has an issue
            Logger.LogWarning("{ExceptionMessage}", e.Message);
        }

        return false;
    }

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

    public async Task<byte[]> GetPatchedCodeAsync(Hash codeHash, byte[] code, int category, bool isSystemContract)
    {
        
        var patchedCode = await _smartContractCodeService.GetSmartContractCodeAsync(codeHash);
        if (patchedCode != null)
        {
            return patchedCode.ToByteArray();
        }

        if (PerformCodePatch(code, category, isSystemContract, out var performPatchedCode))
        {
            await _smartContractCodeService.AddSmartContractCodeAsync(codeHash, ByteString.CopyFrom(performPatchedCode));
            return performPatchedCode;
        }

        throw new ApplicationException($"Cannot get patched code: {codeHash.ToHex()}");
    }
}