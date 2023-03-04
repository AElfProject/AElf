using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Kernel.Infrastructure;

namespace AElf.Kernel.Blockchain.Domain;

public interface ISmartContractCodeManager
{
    Task<Hash> AddSmartContractCodeAsync(SmartContractCode smartContractCode);
    Task<SmartContractCode> GetSmartContractCodeAsync(Hash codeHash);
}

public class SmartContractCodeManager : ISmartContractCodeManager
{
    private readonly IBlockchainStore<SmartContractCode> _smartContractCodeStore;

    public SmartContractCodeManager(IBlockchainStore<SmartContractCode> smartContractCodeStore)
    {
        _smartContractCodeStore = smartContractCodeStore;
    }
    
    public async Task<Hash> AddSmartContractCodeAsync(SmartContractCode smartContractCode)
    {
        var originCodeHash = smartContractCode.CodeHash;
        await _smartContractCodeStore.SetAsync(GetStringKey(originCodeHash), smartContractCode);
        return originCodeHash;
    }

    public async Task<SmartContractCode> GetSmartContractCodeAsync(Hash codeHash)
    {
        return await _smartContractCodeStore.GetAsync(GetStringKey(codeHash));
    }

    private string GetStringKey(Hash codeHash)
    {
        return codeHash.ToStorageKey();
    }
}