using AElf.Contracts.MultiToken;
using AElf.Kernel;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Runtime.WebAssembly.TransactionPayment;
using AElf.Runtime.WebAssembly.TransactionPayment.Extensions;
using AElf.Standards.ACS0;
using AElf.Types;
using Google.Protobuf.Reflection;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.Runtime.WebAssembly;

internal class ChargeGasFeePostExecutionPlugin : IPostExecutionPlugin, ISingletonDependency
{
    private readonly IContractReaderFactory<TokenContractImplContainer.TokenContractImplStub>
        _tokenContractReaderFactory;

    private readonly IContractReaderFactory<ACS0Container.ACS0Stub>
        _genesisContractReaderFactory;

    private readonly ISmartContractAddressService _smartContractAddressService;
    private readonly IFeeService _feeService;
    private readonly WebAssemblyRuntimeOption _options;

    public ChargeGasFeePostExecutionPlugin(
        IContractReaderFactory<TokenContractImplContainer.TokenContractImplStub> tokenContractReaderFactory,
        IContractReaderFactory<ACS0Container.ACS0Stub> genesisContractReaderFactory,
        ISmartContractAddressService smartContractAddressService, IFeeService feeService,
        IOptionsSnapshot<WebAssemblyRuntimeOption> options)
    {
        _tokenContractReaderFactory = tokenContractReaderFactory;
        _genesisContractReaderFactory = genesisContractReaderFactory;
        _smartContractAddressService = smartContractAddressService;
        _feeService = feeService;
        _options = options.Value;
    }

    public async Task<IEnumerable<Transaction>> GetPostTransactionsAsync(IReadOnlyList<ServiceDescriptor> descriptors,
        ITransactionContext transactionContext)
    {
        if (!_options.IsChargeGasFee)
        {
            return new List<Transaction>();
        }

        var chainContext = new ChainContext
        {
            BlockHash = transactionContext.PreviousBlockHash,
            BlockHeight = transactionContext.BlockHeight - 1
        };

        var genesisContractAddress = _smartContractAddressService.GetZeroSmartContractAddress();
        var contractReaderContext = new ContractReaderContext
        {
            BlockHash = chainContext.BlockHash,
            BlockHeight = chainContext.BlockHeight,
            ContractAddress = genesisContractAddress,
            Sender = transactionContext.Transaction.From
        };
        var genesisStub = _genesisContractReaderFactory.Create(contractReaderContext);
        var contractInfo = await genesisStub.GetContractInfo.CallAsync(transactionContext.Transaction.To);
        if (contractInfo.Category != KernelConstants.WasmRunnerCategory)
        {
            return new List<Transaction>();
        }

        // Generate token contract stub.
        var tokenContractAddress =
            await _smartContractAddressService.GetAddressByContractNameAsync(chainContext,
                TokenSmartContractAddressNameProvider.StringName);
        if (tokenContractAddress == null) return new List<Transaction>();

        contractReaderContext.ContractAddress = tokenContractAddress;
        var tokenStub = _tokenContractReaderFactory.Create(contractReaderContext);

        var weight = transactionContext.Trace.GetConsumedGasFee();
        if (weight == null)
        {
            weight = transactionContext.Trace.GetEstimatedGasFee();
            if (weight == null)
            {
                throw new WebAssemblyRuntimePaymentException("Gas fee charge related log event not found.");
            }
        }

        var fee = _feeService.CalculateFees(weight);
        return new[]
        {
            tokenStub.Transfer.GetTransaction(new TransferInput
            {
                // TODO: Transfer to another address like profit pool
                To = tokenContractAddress,
                Amount = fee,
                Symbol = "ELF"
            })
        };
    }
}