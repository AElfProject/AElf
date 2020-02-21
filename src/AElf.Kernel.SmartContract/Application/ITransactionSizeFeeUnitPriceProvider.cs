using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Sdk.CSharp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application
{
    //TODO: should not be here
    public interface ITransactionSizeFeeUnitPriceProvider
    {
        void SetUnitPrice(long unitPrice,BlockIndex blockIndex);
        Task<long> GetUnitPriceAsync(IChainContext chainContext);
        
        //TODO: remove fork logic
        void RemoveForkCache(List<BlockIndex> blockIndexes);
        void SetIrreversedCache(List<BlockIndex> blockIndexes);
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

        public void SetUnitPrice(long unitPrice,BlockIndex blockIndex)
        {
            Logger.LogError("Set tx size unit price wrongly.");
            _unitPrice = unitPrice;
        }

        public Task<long> GetUnitPriceAsync(IChainContext chainContext)
        {
            Logger.LogError("Get tx size unit price wrongly.");
            return Task.FromResult(_unitPrice);
        }

        public void RemoveForkCache(List<BlockIndex> blockIndexes)
        {
            throw new System.NotImplementedException();
        }

        public void SetIrreversedCache(List<BlockIndex> blockIndexes)
        {
            throw new System.NotImplementedException();
        }
    }
}