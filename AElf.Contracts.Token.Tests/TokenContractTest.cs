using System;
using System.IO;
using AElf.Kernel;
using AElf.Types.CSharp;
using AElf.SmartContract;
using Xunit.Frameworks.Autofac;
using Xunit;
using ServiceStack;

namespace AElf.Contracts.Token.Tests
{
    [UseAutofacTestFramework]
    public class TokenContractTest
    {
        private ContractZeroShim _contractZero;
        private TokenContractShim _contract;
        private MockSetup _mock;

        private IExecutive Executive { get; set; }

        public byte[] Code
        {
            get
            {
                byte[] code = null;
                using (FileStream file = File.OpenRead(System.IO.Path.GetFullPath("../../../../AElf.Contracts.Token/bin/Debug/netstandard2.0/AElf.Contracts.Token.dll")))
                {
                    code = file.ReadFully();
                }
                return code;
            }
        }
        
        public TokenContractTest(MockSetup mock)
        {
            _mock = mock;
            Init();
        }

        private void Init()
        {
            _contractZero = new ContractZeroShim(_mock);
            _contractZero.DeploySmartContract(0, Code);
            var address = new Hash(_contractZero.TransactionContext.Trace.RetVal.Data.DeserializeToBytes());
            _contract = new TokenContractShim(_mock, address);
        }

        [Fact]
        public void Test()
        {
            _contractZero.GetContractOwner(_contract.Address);
            
            // Initialize
            _contract.Initialize("ELF", "AElf Token", 1000000000, 2);
            Assert.True(string.IsNullOrEmpty(_contract.TransactionContext.Trace.StdErr));
            Assert.True(_contract.TransactionContext.Trace.IsSuccessful());
            
            // Basic info query
            Assert.Equal("ELF", _contract.Symbol());
            Assert.Equal("AElf Token", _contract.TokenName());
            Assert.Equal((ulong)1000000000, _contract.TotalSupply());
            Assert.Equal((uint)2, _contract.Decimals());
            
            // Cannot Initialize more than one time
            _contract.Initialize("ELF", "AElf Token", 1000000000, 2);
            Assert.False(_contract.TransactionContext.Trace.IsSuccessful());
        }
    }
}