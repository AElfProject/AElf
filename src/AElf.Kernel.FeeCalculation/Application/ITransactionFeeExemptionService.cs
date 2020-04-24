using System.Threading.Tasks;
using AElf.Types;

namespace AElf.Kernel.FeeCalculation.Application
{
    public interface ITransactionFeeExemptionService
    {
        bool IsFree(IChainContext chainContext, Transaction transaction);
    }
}