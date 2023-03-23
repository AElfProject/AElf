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
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee;

internal class MethodFeeChargedPreExecutionPluginBase : SmartContractExecutionPluginBase, IPreExecutionPlugin,
    ISingletonDependency
{
    private readonly IContractReaderFactory<TokenContractImplContainer.TokenContractImplStub>
        _contractReaderFactory;

    private readonly ISmartContractAddressService _smartContractAddressService;
    private readonly ITransactionSizeFeeSymbolsProvider _transactionSizeFeeSymbolsProvider;
    private readonly IPrimaryTokenFeeService _txFeeService;

    public MethodFeeChargedPreExecutionPluginBase(ISmartContractAddressService smartContractAddressService,
        IPrimaryTokenFeeService txFeeService,
        ITransactionSizeFeeSymbolsProvider transactionSizeFeeSymbolsProvider,
        IContractReaderFactory<TokenContractImplContainer.TokenContractImplStub> contractReaderFactory,
        string acsSymbol) : base(acsSymbol)
    {
        _smartContractAddressService = smartContractAddressService;
        _txFeeService = txFeeService;
        _transactionSizeFeeSymbolsProvider = transactionSizeFeeSymbolsProvider;
        _contractReaderFactory = contractReaderFactory;
    }

    public ILogger<MethodFeeChargedPreExecutionPluginBase> Logger { get; set; }

    protected virtual bool IsApplicableToTransaction(IReadOnlyList<ServiceDescriptor> descriptors, Transaction transaction,
        Address tokenContractAddress)
    {
        return false;
    }

    protected virtual bool IsExemptedTransaction(Transaction transaction, Address tokenContractAddress,
        TokenContractImplContainer.TokenContractImplStub tokenStub)
    {
        return false;
    }

    protected virtual Transaction GetTransaction(TokenContractImplContainer.TokenContractImplStub tokenStub,
        ChargeTransactionFeesInput chargeTransactionFeesInput)
    {
        return new Transaction();
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

            var tokenContractAddress = await _smartContractAddressService.GetAddressByContractNameAsync(
                chainContext,
                TokenSmartContractAddressNameProvider.StringName);

            if (transactionContext.BlockHeight < AElfConstants.GenesisBlockHeight + 1 ||
                tokenContractAddress == null)
                return new List<Transaction>();

            if (!IsApplicableToTransaction(descriptors, transactionContext.Transaction, tokenContractAddress))
                return new List<Transaction>();

            var tokenStub = _contractReaderFactory.Create(new ContractReaderContext
            {
                Sender = transactionContext.Transaction.From,
                ContractAddress = tokenContractAddress,
                RefBlockNumber = transactionContext.Transaction.RefBlockNumber
            });

            if (IsExemptedTransaction(transactionContext.Transaction, tokenContractAddress, tokenStub))
                return new List<Transaction>();

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
                foreach (var transactionSizeFeeSymbol in transactionSizeFeeSymbols.TransactionSizeFeeSymbolList)
                    chargeTransactionFeesInput.SymbolsToPayTxSizeFee.Add(new SymbolToPayTxSizeFee
                    {
                        TokenSymbol = transactionSizeFeeSymbol.TokenSymbol,
                        BaseTokenWeight = transactionSizeFeeSymbol.BaseTokenWeight,
                        AddedTokenWeight = transactionSizeFeeSymbol.AddedTokenWeight
                    });

            var chargeFeeTransaction = GetTransaction(tokenStub, chargeTransactionFeesInput);
            return new List<Transaction>
            {
                chargeFeeTransaction
            };
        }
        catch (Exception e)
        {
            Logger.LogError(
                $"Failed to generate ChargeTransactionFees tx.Transaction to: {transactionContext.Transaction.To},transation method name: {transactionContext.Transaction.MethodName}. Error message: {e.Message}");
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