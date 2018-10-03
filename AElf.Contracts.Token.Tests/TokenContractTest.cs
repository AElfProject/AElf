using System;
using System.IO;
using AElf.Common;
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
            _contract = new TokenContractShim(_mock);
        }

        [Fact]
        public void Test()
        {
            _contract.GetContractOwner(Address.FromBytes(_mock.ChainId1.CalculateHashWith(SmartContractType.BasicContractZero.ToString())));
            Assert.True(_contract.TransactionContext.Trace.StdErr.IsNullOrEmpty());
            var owner = _contract.TransactionContext.Trace.RetVal.Data.DeserializeToPbMessage<Address>();

            Assert.Equal(_contract.Sender, owner);
            // Initialize
            _contract.Initialize("ELF", "AElf Token", 1000000000, 2);
            Assert.True(string.IsNullOrEmpty(_contract.TransactionContext.Trace.StdErr));
            Assert.True(_contract.TransactionContext.Trace.IsSuccessful());

            // Basic info query
            Assert.Equal("ELF", _contract.Symbol());
            Assert.Equal("AElf Token", _contract.TokenName());
            Assert.Equal((ulong) 1000000000, _contract.TotalSupply());
            Assert.Equal((uint) 2, _contract.Decimals());

            // Cannot Initialize more than one time
            try
            {
                _contract.Initialize("ELF", "AElf Token", 1000000000, 2);
            }
            catch (Exception e)
            {
                Assert.Equal(ExecutionStatus.ContractError, _contract.TransactionContext.Trace.ExecutionStatus);
            }
        }
    }
}