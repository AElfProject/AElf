using System.IO;
using AElf.SmartContract;
using AElf.Types.CSharp;
using Xunit.Frameworks.Autofac;
using Xunit;
using AElf.Common;

namespace AElf.Contracts.Genesis.Tests
{
    [UseAutofacTestFramework]
    public class ContractZeroTest
    {
        private TestContractShim _contractShim;

        private IExecutive Executive { get; set; }

        private byte[] Code
        {
            get
            {
                var filePath = Path.GetFullPath("../../../../AElf.Contracts.Token/bin/Debug/netstandard2.0/AElf.Contracts.Token.dll");
                return File.ReadAllBytes(filePath);
            }
        }
        private byte[] CodeNew
        {
            get
            {
                var filePath = Path.GetFullPath("../../../../AElf.Benchmark.TestContract/bin/Debug/netstandard2.0/AElf.Benchmark.TestContract.dll");
                return File.ReadAllBytes(filePath);
            }
        }
        
        public ContractZeroTest(TestContractShim contractShim)
        {
            _contractShim = contractShim;
        }

        [Fact]
        public void Test()
        {
            // deploy contract
             _contractShim.DeploySmartContract(0, Code);
            Assert.NotNull(_contractShim.TransactionContext.Trace.RetVal);
            
            // get the address of deployed contract
            var address = Address.FromBytes(_contractShim.TransactionContext.Trace.RetVal.Data.DeserializeToBytes());
            
            // query owner
            _contractShim.GetContractOwner(address);
            var owner = _contractShim.TransactionContext.Trace.RetVal.Data.DeserializeToPbMessage<Address>();
            Assert.Equal(_contractShim.Sender, owner);
            
            _contractShim.UpdateSmartContract(address, CodeNew);
            Assert.Equal(address, _contractShim.TransactionContext.Trace.RetVal.Data.DeserializeToPbMessage<Address>());
            
            // chang owner and query again, owner will be new owner
            var newOwner = Address.Generate();
            _contractShim.ChangeContractOwner(address, newOwner);
            _contractShim.GetContractOwner(address);
            var queryNewOwner = _contractShim.TransactionContext.Trace.RetVal.Data.DeserializeToPbMessage<Address>();
            Assert.Equal(newOwner, queryNewOwner);
            
            _contractShim.UpdateSmartContract(address, CodeNew);
            Assert.NotNull(_contractShim.TransactionContext.Trace.RetVal.Data.DeserializeToPbMessage<Hash>());
        }
    }
}