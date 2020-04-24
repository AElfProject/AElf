using System.Collections.Generic;
using System.Linq;
using AElf.Types;

namespace AElf.Kernel.FeeCalculation.Application
{
    public class TransactionFeeExemptionService : ITransactionFeeExemptionService
    {
        private readonly IEnumerable<IChargeFeeStrategy> _chargeFeeStrategies;

        public TransactionFeeExemptionService(IEnumerable<IChargeFeeStrategy> chargeFeeStrategies)
        {
            _chargeFeeStrategies = chargeFeeStrategies;
        }

        public bool IsFree(IChainContext chainContext,Transaction transaction)
        {
            var usefulStrategies = _chargeFeeStrategies.Where(chargeFeeStrategy =>
                transaction.To == (chargeFeeStrategy.GetContractAddress(chainContext)) &&
                (transaction.MethodName == chargeFeeStrategy.MethodName ||
                 chargeFeeStrategy.MethodName == string.Empty)).ToList();
            return usefulStrategies.Any() &&
                   usefulStrategies.Any(chargeFeeStrategy => chargeFeeStrategy.IsFree(transaction));
        }
    }
}