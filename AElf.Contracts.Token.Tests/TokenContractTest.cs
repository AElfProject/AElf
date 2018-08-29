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

        
        public TokenContractTest(MockSetup mock)
        {
            _mock = mock;
            Init();
        }

        private void Init()
        {
            _contract = new TokenContractShim(_mock, 
                new Hash(_mock.ChainId1.CalculateHashWith(SmartContractType.TokenContract.ToString())).ToAccount());
        }

        // TODO: To fix
        [Fact(Skip = "")]
        public void Test()
        {
            /*/*_contract.GetContractOwner(new Hash(_mock.ChainId1.CalculateHashWith(SmartContractType.BasicContractZero.ToString())).ToAccount());
            Assert.Null(_contract.TransactionContext.Trace.StdErr);
            var owner = _contract.TransactionContext.Trace.RetVal.Data.DeserializeToPbMessage<Hash>();

            Assert.Equal(_contract.Sender, owner);#1#
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
            Assert.False(_contract.TransactionContext.Trace.IsSuccessful());*/
        }
    }
}