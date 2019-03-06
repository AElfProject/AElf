using System;

namespace AElf.Kernel.Blockchain.Application
{
    public interface IExtraDataOrderService
    {
        void AddExtraDataProvider(Type extraDataProviderType);
        int GetExtraDataProviderOrder(Type extraDataProviderType);
    }
}