using System;
using System.IO;
using AElf.Common;
using AElf.Kernel.Blockchain.Infrastructure;
using Google.Protobuf;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Infrastructure
{
    public interface IDefaultContractZeroCodeProvider
    {
        SmartContractRegistration DefaultContractZeroRegistration { get; set; }

        Address ContractZeroAddress { get; }

        void SetDefaultContractZeroRegistrationByType(Type defaultZero);
    }

    public class DefaultContractZeroCodeProvider : IDefaultContractZeroCodeProvider, ISingletonDependency
    {
        private readonly IStaticChainInformationProvider _staticChainInformationProvider;

        public DefaultContractZeroCodeProvider(IStaticChainInformationProvider staticChainInformationProvider)
        {
            _staticChainInformationProvider = staticChainInformationProvider;
        }

        public SmartContractRegistration DefaultContractZeroRegistration { get; set; }
        public Address ContractZeroAddress => _staticChainInformationProvider.ZeroSmartContractAddress;

        public void SetDefaultContractZeroRegistrationByType(Type defaultZero)
        {
            var code = File.ReadAllBytes(defaultZero.Assembly.Location);
            DefaultContractZeroRegistration = new SmartContractRegistration()
            {
                Category = 2,
                Code = ByteString.CopyFrom(code),
                CodeHash = Hash.FromRawBytes(code)
            };
        }
    }
}