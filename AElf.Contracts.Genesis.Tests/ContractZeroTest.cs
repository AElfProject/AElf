using System.IO;
using AElf.Kernel;
using AElf.SmartContract;
using AElf.Types.CSharp;
using Xunit.Frameworks.Autofac;
using Xunit;
using ServiceStack;
using AElf.Common;
using Google.Protobuf;

namespace AElf.Contracts.Genesis.Tests
{
    [UseAutofacTestFramework]
    public class ContractZeroTest
    {
        private TestContractShim _contractShim;
        private MockSetup _mock;

        private IExecutive Executive { get; set; }

        private byte[] Code
        {
            get
            {
                byte[] code;
                using (var file = File.OpenRead(Path.GetFullPath("../../../../AElf.Contracts.Token/bin/Debug/netstandard2.0/AElf.Contracts.Token.dll")))
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
            var address = Address.FromRawBytes(_contractShim.TransactionContext.Trace.RetVal.Data.DeserializeToBytes());
            
            // query owner
            _contractShim.GetContractOwner(address);
            var owner = _contractShim.TransactionContext.Trace.RetVal.Data.DeserializeToPbMessage<Address>();
            Assert.Equal(_contractShim.Sender, owner);

            // chang owner and query again, owner will be new owner
            var newOwner = Address.FromRawBytes(Hash.Generate().ToByteArray());
            _contractShim.ChangeContractOwner(address, newOwner);
            _contractShim.GetContractOwner(address);
            var queryNewOwner = _contractShim.TransactionContext.Trace.RetVal.Data.DeserializeToPbMessage<Address>();
            Assert.Equal(newOwner, queryNewOwner);     
        }
    }
}