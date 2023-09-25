using AElf.Contracts.MultiToken;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.Token;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.DependencyInjection;

namespace AElf.Runtime.WebAssembly;

internal class CSharpContractReader : ICSharpContractReader, ISingletonDependency
{
    private readonly IHostSmartContractBridgeContextService _hostSmartContractBridgeContextService;
    private readonly IBlockchainService _blockchainService;
    private readonly ISmartContractAddressProvider _smartContractAddressProvider;
    private readonly ISmartContractExecutiveProvider _smartContractExecutiveProvider;
    private readonly ISmartContractRegistrationProvider _smartContractRegistrationProvider;

    public CSharpContractReader(IHostSmartContractBridgeContextService hostSmartContractBridgeContextService,
        IBlockchainService blockchainService, ISmartContractAddressProvider smartContractAddressProvider,
        ISmartContractExecutiveProvider smartContractExecutiveProvider,
        ISmartContractRegistrationProvider smartContractRegistrationProvider)
    {
        _hostSmartContractBridgeContextService = hostSmartContractBridgeContextService;
        _blockchainService = blockchainService;
        _smartContractAddressProvider = smartContractAddressProvider;
        _smartContractExecutiveProvider = smartContractExecutiveProvider;
        _smartContractRegistrationProvider = smartContractRegistrationProvider;
    }

    public async Task<long> GetBalanceAsync(Address owner, string? symbol = null)
    {
        var chain = await _blockchainService.GetChainAsync();
        var smartContractAddress = await _smartContractAddressProvider.GetSmartContractAddressAsync(new ChainContext
            {
                BlockHeight = chain.BestChainHeight,
                BlockHash = chain.BestChainHash
            }, TokenSmartContractAddressNameProvider.StringName);
        var tokenContractAddress = smartContractAddress.Address;
        if (!_smartContractExecutiveProvider.TryGetValue(tokenContractAddress, out var executives)
            || !executives.TryTake(out var executive))
        {
            return 0;
        }

        var hostSmartContractBridgeContext = _hostSmartContractBridgeContextService.Create();
        executive.SetHostSmartContractBridgeContext(hostSmartContractBridgeContext);
        await executive.ApplyAsync(new TransactionContext
        {
            Transaction = new Transaction
            {
                To = tokenContractAddress,
                From = owner,
                Params = new GetBalanceInput
                {
                    Symbol = "ELF",
                    Owner = owner
                }.ToByteString()
            },
        });
        var output = new GetBalanceOutput();
        output.MergeFrom(hostSmartContractBridgeContext.TransactionContext.Trace.ReturnValue);
        return output.Balance;
    }
}