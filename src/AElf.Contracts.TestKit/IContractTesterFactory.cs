using System;
using AElf.Cryptography.ECDSA;
using AElf.CSharp.Core;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Contracts.TestKit
{
    public interface IContractTesterFactory
    {
        T Create<T>(Address contractAddress, ECKeyPair senderKey) where T : ContractStubBase, new();
    }

    public class ContractTesterFactory : IContractTesterFactory, ITransientDependency
    {
        private readonly IServiceProvider _serviceProvider;

        public ContractTesterFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public T Create<T>(Address contractAddress, ECKeyPair senderKey) where T : ContractStubBase, new()
        {
            return new T
            {
                __factory = new MethodStubFactory(_serviceProvider)
                {
                    ContractAddress = contractAddress,
                    KeyPair = senderKey
                }
            };
        }
    }
}