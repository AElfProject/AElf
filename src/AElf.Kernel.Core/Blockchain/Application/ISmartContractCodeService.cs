using AElf.Kernel.Blockchain.Domain;

namespace AElf.Kernel.Blockchain.Application;

public interface ISmartContractCodeService
{
    Task<ByteString> GetSmartContractCodeAsync(Hash codeHash);

    Task AddSmartContractCodeAsync(Hash codeHash, ByteString patchedCode);
}

public class SmartContractCodeService : ISmartContractCodeService, ITransientDependency
{
    private readonly ISmartContractCodeManager _smartContractCodeManager;

    public SmartContractCodeService(ISmartContractCodeManager smartContractCodeManager)
    {
        _smartContractCodeManager = smartContractCodeManager;
    }

    public async Task<ByteString> GetSmartContractCodeAsync(Hash codeHash)
    {
        var code = await _smartContractCodeManager.GetSmartContractCodeAsync(codeHash);
        return code?.PatchedCode;
    }
    
    public async Task AddSmartContractCodeAsync(Hash codeHash, ByteString patchedCode)
    {
        await _smartContractCodeManager.AddSmartContractCodeAsync(new SmartContractCode
        {
            CodeHash = codeHash,
            PatchedCode = patchedCode
        });
    }
}