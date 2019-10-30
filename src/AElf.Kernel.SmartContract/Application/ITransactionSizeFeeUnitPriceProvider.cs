using System.Threading.Tasks;

namespace AElf.Kernel.SmartContract.Application
{
    public interface ITransactionSizeFeeUnitPriceProvider
    {
        void SetUnitPrice(long unitPrice);
        Task<long> GetUnitPriceAsync();
    }

    public class DefaultTransactionSizeFeeUnitPriceProvider : ITransactionSizeFeeUnitPriceProvider
    {
        private long _unitPrice;

        public void SetUnitPrice(long unitPrice)
        {
            _unitPrice = unitPrice;
        }

        public Task<long> GetUnitPriceAsync()
        {
            return Task.FromResult(_unitPrice);
        }
    }
}