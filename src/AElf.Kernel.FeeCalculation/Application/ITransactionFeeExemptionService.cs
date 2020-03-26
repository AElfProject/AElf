using AElf.Types;

namespace AElf.Kernel.FeeCalculation.Application
{
    public interface ITransactionFeeExemptionService
    {
        bool IsFree(Transaction transaction);
    }
}