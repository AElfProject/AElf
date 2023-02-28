using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Kernel.FeeCalculation.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee;

internal class UserFeeChargePreExecutionPlugin : SmartContractExecutionPluginBase, IPreExecutionPlugin,
    ISingletonDependency
{
    private readonly IContractReaderFactory<TokenContractImplContainer.TokenContractImplStub>
        _contractReaderFactory;

    public ILogger<UserFeeChargePreExecutionPlugin> Logger { get; set; }
    private readonly ISmartContractAddressService _smartContractAddressService;
    private readonly IPrimaryTokenFeeService _txFeeService;
    private readonly ITransactionSizeFeeSymbolsProvider _transactionSizeFeeSymbolsProvider;


    public UserFeeChargePreExecutionPlugin(ISmartContractAddressService smartContractAddressService,
        IContractReaderFactory<TokenContractImplContainer.TokenContractImplStub> contractReaderFactory,
        IPrimaryTokenFeeService txFeeService,
        ITransactionSizeFeeSymbolsProvider transactionSizeFeeSymbolsProvider) : base("acs12")
    {
        _smartContractAddressService = smartContractAddressService;
        _contractReaderFactory = contractReaderFactory;
        _txFeeService = txFeeService;
        _transactionSizeFeeSymbolsProvider = transactionSizeFeeSymbolsProvider;
        Logger = NullLogger<UserFeeChargePreExecutionPlugin>.Instance;
    }

    public async Task<IEnumerable<Transaction>> GetPreTransactionsAsync(IReadOnlyList<ServiceDescriptor> descriptors,
        ITransactionContext transactionContext)
    {
        try
        {
            var chainContext = new ChainContext
            {
                BlockHash = transactionContext.PreviousBlockHash,
                BlockHeight = transactionContext.BlockHeight - 1
            };
            var tokenContractAddress =
                await _smartContractAddressService.GetAddressByContractNameAsync(chainContext,
                    TokenSmartContractAddressNameProvider.StringName);
            if (transactionContext.BlockHeight < AElfConstants.GenesisBlockHeight + 1 || tokenContractAddress == null)
            {
                return new List<Transaction>();
            }

            if (!IsTargetAcsSymbol(descriptors))
                return new List<Transaction>();
            
            var tokenStub = _contractReaderFactory.Create(new ContractReaderContext
            {
                Sender = transactionContext.Transaction.From,
                ContractAddress = tokenContractAddress,
                RefBlockNumber = transactionContext.Transaction.RefBlockNumber
            });
            if (transactionContext.Transaction.To == tokenContractAddress
                && (transactionContext.Transaction.MethodName == nameof(tokenStub.ChargeUserTransactionFees) ||
                transactionContext.Transaction.MethodName == nameof(tokenStub.ChargeTransactionFees)))
            {
                // Skip ChargeUserTransactionFees itself and ChargeTransactionFees
                return new List<Transaction>();
            }

            var txCost = await _txFeeService.CalculateFeeAsync(transactionContext, chainContext);
            var chargeTransactionFeesInput = new ChargeTransactionFeesInput
            {
                MethodName = transactionContext.Transaction.MethodName,
                ContractAddress = transactionContext.Transaction.To,
                TransactionSizeFee = txCost
            };
            var transactionSizeFeeSymbols =
                await _transactionSizeFeeSymbolsProvider.GetTransactionSizeFeeSymbolsAsync(chainContext);
            if (transactionSizeFeeSymbols != null)
            {
                foreach (var transactionSizeFeeSymbol in transactionSizeFeeSymbols.TransactionSizeFeeSymbolList)
                {
                    chargeTransactionFeesInput.SymbolsToPayTxSizeFee.Add(new SymbolToPayTxSizeFee
                    {
                        TokenSymbol = transactionSizeFeeSymbol.TokenSymbol,
                        BaseTokenWeight = transactionSizeFeeSymbol.BaseTokenWeight,
                        AddedTokenWeight = transactionSizeFeeSymbol.AddedTokenWeight
                    });
                }
            }

            var chargeFeeTransaction = tokenStub.ChargeUserTransactionFees.GetTransaction(chargeTransactionFeesInput);
            return new List<Transaction>
            {
                chargeFeeTransaction
            };
        }
        catch (Exception e)
        {
            Logger.LogError($"Failed to generate ChargeUserTransactionFees tx. {e.Message}");
            throw;
        }
    }

    public bool IsStopExecuting(ByteString txReturnValue, out string preExecutionInformation)
    {
        var chargeTransactionFeesOutput = new ChargeTransactionFeesOutput();
        chargeTransactionFeesOutput.MergeFrom(txReturnValue);
        preExecutionInformation = chargeTransactionFeesOutput.ChargingInformation;
        return !chargeTransactionFeesOutput.Success;
    }
}