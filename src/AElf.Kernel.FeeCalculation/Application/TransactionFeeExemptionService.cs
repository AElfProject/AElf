using System.Collections.Generic;
using System.Linq;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.FeeCalculation.Application
{
    public class TransactionFeeExemptionService : ITransactionFeeExemptionService
    {
        private readonly IEnumerable<IChargeFeeStrategy> _chargeFeeStrategies;

        public TransactionFeeExemptionService(IEnumerable<IChargeFeeStrategy> chargeFeeStrategies)
        {
            _chargeFeeStrategies = chargeFeeStrategies;
        }

        public bool IsFree(Transaction transaction)
        {
            var usefulStrategies = _chargeFeeStrategies.Where(chargeFeeStrategy =>
                transaction.To == chargeFeeStrategy.ContractAddress &&
                (transaction.MethodName == chargeFeeStrategy.MethodName ||
                 chargeFeeStrategy.MethodName == string.Empty)).ToList();
            return usefulStrategies.Any() &&
                   usefulStrategies.Any(chargeFeeStrategy => chargeFeeStrategy.IsFree(transaction));
        }
    }
}