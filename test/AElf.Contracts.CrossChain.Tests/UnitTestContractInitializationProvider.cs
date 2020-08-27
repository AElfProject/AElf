using System.Collections.Generic;
using AElf.CrossChain;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Types;

namespace AElf.Contracts.CrossChain.Tests
{
    public class UnitTestTokenContractInitializationProvider : IContractInitializationProvider
    {
        public Hash SystemSmartContractName { get; } = TokenSmartContractAddressNameProvider.Name;
        public string ContractCodeName { get; } = "AElf.Contracts.MultiToken";
        
        public List<ContractInitializationMethodCall> GetInitializeMethodList(byte[] contractCode)
        {
            return new List<ContractInitializationMethodCall>();
        }
    }
    
    public class UnitTestCrossChainContractInitializationProvider : IContractInitializationProvider
    {
        public Hash SystemSmartContractName { get; } = CrossChainSmartContractAddressNameProvider.Name;
        public string ContractCodeName { get; } = "AElf.Contracts.CrossChain";
        
        public List<ContractInitializationMethodCall> GetInitializeMethodList(byte[] contractCode)
        {
            return new List<ContractInitializationMethodCall>();
        }
    }
}