using AElf.Contracts.MultiToken;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Runtime.CSharp;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.Runtime.WebAssembly;

internal class CSharpContractReader : ICSharpContractReader, ISingletonDependency
{
    private readonly IHostSmartContractBridgeContextService _hostSmartContractBridgeContextService;
    private readonly IBlockchainService _blockchainService;
    private readonly ISmartContractAddressProvider _smartContractAddressProvider;
    private readonly ISmartContractExecutiveProvider _smartContractExecutiveProvider;
    private readonly ISmartContractRegistrationProvider _smartContractRegistrationProvider;
    private readonly ITransactionContextFactory _transactionContextFactory;
    private readonly RunnerOptions _runnerOptions;

    public CSharpContractReader(IHostSmartContractBridgeContextService hostSmartContractBridgeContextService,
        IBlockchainService blockchainService, ISmartContractAddressProvider smartContractAddressProvider,
        ISmartContractExecutiveProvider smartContractExecutiveProvider,
        ISmartContractRegistrationProvider smartContractRegistrationProvider,
        ITransactionContextFactory transactionContextFactory,
        IOptionsSnapshot<RunnerOptions> runnerOptions)
    {
        _hostSmartContractBridgeContextService = hostSmartContractBridgeContextService;
        _blockchainService = blockchainService;
        _smartContractAddressProvider = smartContractAddressProvider;
        _smartContractExecutiveProvider = smartContractExecutiveProvider;
        _smartContractRegistrationProvider = smartContractRegistrationProvider;
        _transactionContextFactory = transactionContextFactory;
        _runnerOptions = runnerOptions.Value;
    }

    public async Task<long> GetBalanceAsync(Address from, Address owner, string? symbol = null)
    {
        var output = await ExecuteCSharpContract<GetBalanceOutput>(from,
            TokenSmartContractAddressNameProvider.StringName,
            "GetBalance", new GetBalanceInput
            {
                Symbol = "ELF",
                Owner = owner
            }.ToByteString());
        return output.Balance;
    }

    // public async Task Transfer(Address from, Address to, long amount, string? symbol = null)
    // {
    //     await ExecuteCSharpContract(from, TokenSmartContractAddressNameProvider.StringName, "Transfer",
    //         new TransferInput
    //         {
    //             Symbol = "ELF",
    //             Amount = amount,
    //             To = to
    //         }.ToByteString());
    // }

    private async Task<T> ExecuteCSharpContract<T>(Address from, string contractName, string methodName,
        ByteString parameter) where T : IMessage<T>, new()
    {
        var txContext = await ExecuteCSharpContract(from, contractName, methodName, parameter);
        var output = new T();
        output.MergeFrom(txContext.Trace.ReturnValue);
        return output;
    }

    private async Task<ITransactionContext> ExecuteCSharpContract(Address from, string contractName, string methodName,
        ByteString parameter)
    {
        var chain = await _blockchainService.GetChainAsync();
        var chainContext = new ChainContext
        {
            BlockHeight = chain.BestChainHeight,
            BlockHash = chain.BestChainHash
        };
        var smartContractAddress = await _smartContractAddressProvider.GetSmartContractAddressAsync(new ChainContext
        {
            BlockHeight = chain.BestChainHeight,
            BlockHash = chain.BestChainHash
        }, contractName);
        var contractAddress = smartContractAddress.Address;
        if (!_smartContractExecutiveProvider.TryGetValue(contractAddress, out var executives)
            || !executives.TryTake(out var executive))
        {
            var registration =
                await _smartContractRegistrationProvider.GetSmartContractRegistrationAsync(chainContext,
                    contractAddress);

            executive = await new CSharpSmartContractRunner(_runnerOptions.SdkDir).RunAsync(registration);
        }

        var hostSmartContractBridgeContext = _hostSmartContractBridgeContextService.Create();
        executive.SetHostSmartContractBridgeContext(hostSmartContractBridgeContext);
        var transaction = new Transaction
        {
            To = contractAddress,
            From = from,
            MethodName = methodName,
            Params = parameter,
            RefBlockNumber = chain.BestChainHeight,
            RefBlockPrefix = BlockHelper.GetRefBlockPrefix(chain.BestChainHash),
        };
        var txContext = _transactionContextFactory.Create(transaction, chainContext);
        await executive.ApplyAsync(txContext);
        return txContext;
    }
}