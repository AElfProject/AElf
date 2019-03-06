using System;
using System.Collections.Generic;

namespace AElf.Kernel.Blockchain.Application
{
    public class ExtraDataOrderService : IExtraDataOrderService
    {
        private readonly Dictionary<Type, int> _ordersDictionary = new Dictionary<Type, int>();

        public void AddExtraDataProvider(Type extraDataProviderType)
        {
            var order = _ordersDictionary.Count;
            _ordersDictionary.Add(extraDataProviderType, order);
        }

        public int GetExtraDataProviderOrder(Type extraDataProviderType)
        {
            return _ordersDictionary[extraDataProviderType];
        }
    }
}