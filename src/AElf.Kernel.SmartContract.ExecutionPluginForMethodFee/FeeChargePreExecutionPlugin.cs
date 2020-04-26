using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Kernel.FeeCalculation.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee
{
    internal class FeeChargePreExecutionPlugin : SmartContractExecutionPluginBase, IPreExecutionPlugin, ISingletonDependency
    {
        private readonly IHostSmartContractBridgeContextService _contextService;
        private readonly IPrimaryTokenSymbolProvider _primaryTokenSymbolProvider;
        private readonly IPrimaryTokenFeeService _txFeeService;
        private readonly ITransactionFeeExemptionService _transactionFeeExemptionService;
        private readonly ITransactionSizeFeeSymbolsProvider _transactionSizeFeeSymbolsProvider;
        private readonly IContractReaderFactory<TokenContractImplContainer.TokenContractImplStub>
            _contractReaderFactory;

        public ILogger<FeeChargePreExecutionPlugin> Logger { get; set; }

        public FeeChargePreExecutionPlugin(IHostSmartContractBridgeContextService contextService,
            IPrimaryTokenSymbolProvider primaryTokenSymbolProvider,
            ITransactionFeeExemptionService transactionFeeExemptionService,
            IPrimaryTokenFeeService txFeeService,
            ITransactionSizeFeeSymbolsProvider transactionSizeFeeSymbolsProvider,
            IContractReaderFactory<TokenContractImplContainer.TokenContractImplStub> contractReaderFactory) :
            base("acs1")
        {
            _contextService = contextService;
            _primaryTokenSymbolProvider = primaryTokenSymbolProvider;
            _txFeeService = txFeeService;
            _transactionSizeFeeSymbolsProvider = transactionSizeFeeSymbolsProvider;
            _contractReaderFactory = contractReaderFactory;
            _transactionFeeExemptionService = transactionFeeExemptionService;
            Logger = NullLogger<FeeChargePreExecutionPlugin>.Instance;
        }

        public async Task<IEnumerable<Transaction>> GetPreTransactionsAsync(
            IReadOnlyList<ServiceDescriptor> descriptors, ITransactionContext transactionContext)
        {
            try
            {
                var context = _contextService.Create();

                var chainContext = new ChainContext
                {
                    BlockHash = transactionContext.PreviousBlockHash,
                    BlockHeight = transactionContext.BlockHeight - 1
                };
                if (_transactionFeeExemptionService.IsFree(chainContext, transactionContext.Transaction))
                {
                    return new List<Transaction>();
                }

                context.TransactionContext = transactionContext;
                var tokenContractAddress = context.GetContractAddressByName(TokenSmartContractAddressNameProvider.StringName);

                if (context.CurrentHeight < AElfConstants.GenesisBlockHeight + 1 || tokenContractAddress == null)
                {
                    return new List<Transaction>();
                }

                if (!IsTargetAcsSymbol(descriptors) && transactionContext.Transaction.To != tokenContractAddress)
                {
                    return new List<Transaction>();
                }

                var tokenStub = _contractReaderFactory.Create(new ContractReaderContext
                {
                    Sender = transactionContext.Transaction.From,
                    ContractAddress = tokenContractAddress
                });
                    
                if (transactionContext.Transaction.To == tokenContractAddress &&
                    transactionContext.Transaction.MethodName == nameof(tokenStub.ChargeTransactionFees))
                {
                    // Skip ChargeTransactionFees itself 
                    return new List<Transaction>();
                }
                
                var txCost = await _txFeeService.CalculateFeeAsync(transactionContext, chainContext);
                var chargeTransactionFeesInput = new ChargeTransactionFeesInput
                {
                    MethodName = transactionContext.Transaction.MethodName,
                    ContractAddress = transactionContext.Transaction.To,
                    TransactionSizeFee = txCost,
                    PrimaryTokenSymbol = await _primaryTokenSymbolProvider.GetPrimaryTokenSymbol(),
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

                var chargeFeeTransaction = tokenStub.ChargeTransactionFees.GetTransaction(chargeTransactionFeesInput);
                return new List<Transaction>
                {
                    chargeFeeTransaction
                };
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Failed to generate ChargeTransactionFees tx.");
                throw;
            }
        }

        public bool IsStopExecuting(ByteString txReturnValue)
        {
            return !BoolValue.Parser.ParseFrom(txReturnValue).Value;
        }
    }
}