using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.SmartContract.Application
{
    public interface ITransactionSizeFeeUnitPriceProvider
    {
        void SetUnitPrice(long unitPrice);
        Task<long> GetUnitPriceAsync();
    }

    /// <summary>
    /// For testing.
    /// </summary>
    public class DefaultTransactionSizeFeeUnitPriceProvider : ITransactionSizeFeeUnitPriceProvider
    {
        private long _unitPrice;

        public ILogger<DefaultTransactionSizeFeeUnitPriceProvider> Logger { get; set; }

        public DefaultTransactionSizeFeeUnitPriceProvider()
        {
            Logger = new NullLogger<DefaultTransactionSizeFeeUnitPriceProvider>();
        }

        public void SetUnitPrice(long unitPrice)
        {
            Logger.LogError("Set tx size unit price wrongly.");
            _unitPrice = unitPrice;
        }

        public Task<long> GetUnitPriceAsync()
        {
            Logger.LogError("Get tx size unit price wrongly.");
            return Task.FromResult(_unitPrice);
        }
    }
}