using System.Linq;
using AElf.Kernel.Miner.Application;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee.FreeFeeTransactions
{
    public class TransactionFeeExemptionService : ITransactionFeeExemptionService, ISingletonDependency
    {
        private readonly IServiceContainer<ISystemTransactionRecognizer> _systemTransactionRecognizers;

        public TransactionFeeExemptionService(
            IServiceContainer<ISystemTransactionRecognizer> systemTransactionRecognizers)
        {
            _systemTransactionRecognizers = systemTransactionRecognizers;
        }

        public bool IsFree(Transaction transaction)
        {
            return _systemTransactionRecognizers.Any(systemTransactionRecognizer =>
                systemTransactionRecognizer.IsSystemTransaction(transaction));
        }
    }
}