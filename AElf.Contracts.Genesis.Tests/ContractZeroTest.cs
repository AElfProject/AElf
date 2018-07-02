using System.IO;
using AElf.Kernel;
using AElf.Types.CSharp;
using Xunit.Frameworks.Autofac;
using Xunit;
using ServiceStack;

namespace AElf.Contracts.Genesis.Tests
{
    [UseAutofacTestFramework]
    public class ContractZeroTest
    {
        private TestContractShim _contractShim;
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
            var address = new Hash(_contractShim.TransactionContext.Trace.RetVal.DeserializeToBytes());
            
            // query owner
            _contractShim.GetContractOwner(address);
            var owner = _contractShim.TransactionContext.Trace.RetVal.DeserializeToPbMessage<Hash>();
            Assert.Equal(_contractShim.Sender, owner);

            // chang owner and query again, owner will be new owner
            var newOwner = Hash.Generate().ToAccount();
            _contractShim.ChangeContractOwner(address, newOwner);
            _contractShim.GetContractOwner(address);
            var queryNewOwner = _contractShim.TransactionContext.Trace.RetVal.DeserializeToPbMessage<Hash>();
            Assert.Equal(newOwner, queryNewOwner);            
        }
    }
}