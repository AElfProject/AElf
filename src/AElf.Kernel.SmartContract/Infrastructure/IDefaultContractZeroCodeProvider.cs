using System;
using System.IO;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Infrastructure
{
    public interface IDefaultContractZeroCodeProvider
    {
        SmartContractRegistration DefaultContractZeroRegistration { get; set; }

        Address ContractZeroAddress { get; }

        void SetDefaultContractZeroRegistrationByType(Type defaultZero);
        
        Address GetZeroSmartContractAddress(int chainId);
    }

    public class DefaultContractZeroCodeProvider : IDefaultContractZeroCodeProvider, ISingletonDependency
    {
        private readonly IStaticChainInformationProvider _staticChainInformationProvider;
        private readonly ContractOptions _contractOptions;

        public DefaultContractZeroCodeProvider(IStaticChainInformationProvider staticChainInformationProvider,
            IOptionsSnapshot<ContractOptions> contractOptions)
        {
            _staticChainInformationProvider = staticChainInformationProvider;
            _contractOptions = contractOptions.Value;
        }

        public SmartContractRegistration DefaultContractZeroRegistration { get; set; }
        public Address ContractZeroAddress => _staticChainInformationProvider.ZeroSmartContractAddress;

        public void SetDefaultContractZeroRegistrationByType(Type defaultZero)
        {
            var dllPath = _contractOptions.GenesisContractDir != null
                ? Path.Combine(_contractOptions.GenesisContractDir, $"{defaultZero.Assembly.GetName().Name}.dll")
                : defaultZero.Assembly.Location;
            var code = File.ReadAllBytes(dllPath);
            DefaultContractZeroRegistration = new SmartContractRegistration()
            {
                Category = KernelConstants.DefaultRunnerCategory,
                Code = ByteString.CopyFrom(code),
                CodeHash = Hash.FromRawBytes(code)
            };
        }

        public Address GetZeroSmartContractAddress(int chainId)
        {
            return _staticChainInformationProvider.GetZeroSmartContractAddress(chainId);
        }
    }
}