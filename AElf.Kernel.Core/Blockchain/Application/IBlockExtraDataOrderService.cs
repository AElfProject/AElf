using System;

namespace AElf.Kernel.Blockchain.Application
{
    public interface IBlockExtraDataOrderService
    {
        void AddExtraDataProvider(Type extraDataProviderType);
        int GetExtraDataProviderOrder(Type extraDataProviderType);
    }
}