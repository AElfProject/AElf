using System.Linq;
using AElf.Kernel.Miner.Application;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee.FreeFeeTransactions
{
    public class TransactionFeeExemptionService : ITransactionFeeExemptionService, ISingletonDependency
    {
        private readonly IServiceContainer<IChargeFeeStrategy> _chargeFeeStrategies;
        private readonly IServiceContainer<ISystemTransactionRecognizer> _systemTransactionRecognizers;

        public TransactionFeeExemptionService(IServiceContainer<IChargeFeeStrategy> chargeFeeStrategies,
            IServiceContainer<ISystemTransactionRecognizer> systemTransactionRecognizers)

        {
            _chargeFeeStrategies = chargeFeeStrategies;
            _systemTransactionRecognizers = systemTransactionRecognizers;
        }

        private bool IsChargeFeeStrategyFree(Transaction transaction)
        {
            var usefulStrategies = _chargeFeeStrategies.Where(chargeFeeStrategy =>
                transaction.To == chargeFeeStrategy.ContractAddress &&
                (transaction.MethodName == chargeFeeStrategy.MethodName ||
                 chargeFeeStrategy.MethodName == string.Empty)).ToList();
            return usefulStrategies.Any() &&
                   usefulStrategies.Any(chargeFeeStrategy => chargeFeeStrategy.IsFree(transaction));
        }
        
        private bool IsSystemTransaction(Transaction transaction)
        {
            return _systemTransactionRecognizers.Any(systemTransactionRecognizer =>
                systemTransactionRecognizer.IsSystemTransaction(transaction));
        }

        public bool IsFree(Transaction transaction)
        {
            return IsChargeFeeStrategyFree(transaction) || IsSystemTransaction(transaction);
        }
    }
}