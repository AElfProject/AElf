using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Kernel.FeeCalculation.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.ExecutionPluginForMethodFee.FreeFeeTransactions;
using AElf.Kernel.Token;
using AElf.Types;
using Google.Protobuf.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee
{
    public class FeeChargePreExecutionPlugin : IPreExecutionPlugin, ISingletonDependency
    {
        private readonly IHostSmartContractBridgeContextService _contextService;
        private readonly IPrimaryTokenSymbolProvider _primaryTokenSymbolProvider;
        private readonly IPrimaryTokenFeeService _txFeeService;
        private readonly ITransactionFeeExemptionService _transactionFeeExemptionService;
        private readonly ITransactionSizeFeeSymbolsProvider _transactionSizeFeeSymbolsProvider;

        public ILogger<FeeChargePreExecutionPlugin> Logger { get; set; }

        public FeeChargePreExecutionPlugin(IHostSmartContractBridgeContextService contextService,
            IPrimaryTokenSymbolProvider primaryTokenSymbolProvider,
            ITransactionFeeExemptionService transactionFeeExemptionService,
            IPrimaryTokenFeeService txFeeService, 
            ITransactionSizeFeeSymbolsProvider transactionSizeFeeSymbolsProvider)
        {
            _contextService = contextService;
            _primaryTokenSymbolProvider = primaryTokenSymbolProvider;
            _txFeeService = txFeeService;
            _transactionSizeFeeSymbolsProvider = transactionSizeFeeSymbolsProvider;
            _transactionFeeExemptionService = transactionFeeExemptionService;
            Logger = NullLogger<FeeChargePreExecutionPlugin>.Instance;
        }

        private static bool IsAcs1(IReadOnlyList<ServiceDescriptor> descriptors)
        {
            return descriptors.Any(service => service.File.GetIdentity() == "acs1");
        }

        public async Task<IEnumerable<Transaction>> GetPreTransactionsAsync(
            IReadOnlyList<ServiceDescriptor> descriptors, ITransactionContext transactionContext)
        {
            try
            {
                var context = _contextService.Create();

                if (_transactionFeeExemptionService.IsFree(transactionContext.Transaction))
                {
                    return new List<Transaction>();
                }

                context.TransactionContext = transactionContext;
                var tokenContractAddress = context.GetContractAddressByName(TokenSmartContractAddressNameProvider.Name);

                if (context.CurrentHeight < Constants.GenesisBlockHeight + 1 || tokenContractAddress == null)
                {
                    return new List<Transaction>();
                }

                if (!IsAcs1(descriptors) && transactionContext.Transaction.To != tokenContractAddress)
                {
                    return new List<Transaction>();
                }

                var tokenStub = GetTokenContractStub(transactionContext.Transaction.From, tokenContractAddress);
                if (transactionContext.Transaction.To == tokenContractAddress &&
                    transactionContext.Transaction.MethodName == nameof(tokenStub.ChargeTransactionFees))
                {
                    // Skip ChargeTransactionFees itself 
                    return new List<Transaction>();
                }
                
                var chainContext = new ChainContext
                {
                    BlockHash = transactionContext.PreviousBlockHash,
                    BlockHeight = transactionContext.BlockHeight - 1
                };
                var txCost = await _txFeeService.CalculateTokenFeeAsync(transactionContext, chainContext);
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

                var chargeFeeTransaction = (await tokenStub.ChargeTransactionFees.SendAsync(chargeTransactionFeesInput))
                    .Transaction;
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

        private static TokenContractContainer.TokenContractStub GetTokenContractStub(Address sender,
            Address contractAddress)
        {
            return new TokenContractContainer.TokenContractStub
            {
                __factory = new TransactionGeneratingOnlyMethodStubFactory
                {
                    Sender = sender,
                    ContractAddress = contractAddress
                }
            };
        }
    }
}