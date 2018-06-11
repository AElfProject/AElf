using System.Threading.Tasks;
using AElf.Contracts.DPoS;
using AElf.Kernel;
using AElf.Kernel.Services;
using Google.Protobuf;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Runtime.CSharp.Tests
{
    [UseAutofacTestFramework]
    // ReSharper disable once InconsistentNaming
    public class DPoSTest
    {
        private readonly DPoSMockSetup _mock;
        private readonly ISmartContractService _service;

        private readonly IExecutive _executive;
        
        public DPoSTest(DPoSMockSetup mock)
        {
            _mock = mock;
            _service = mock.SmartContractService;
            _executive = _service.GetExecutiveAsync(_mock.DPoSContractAddress, _mock.ChainId).Result;
            _executive.SetSmartContractContext(new SmartContractContext
            {
                ChainId = Hash.Zero,
                ContractAddress = Hash.Zero,
                DataProvider = _mock.AccountDataProviderOfAccountZero.GetDataProvider(),
                SmartContractService = _service
            });
        }

        [Fact]
        public async Task GetMiningNodesTest()
        {
            var setMiningNodes = new Transaction
            {
                From = Hash.Zero,
                To = Hash.Zero,
                MethodName = "SetMiningNodes",
                Params = ByteString.CopyFrom(
                    new Parameters().ToByteArray()
                )
            };
            
            await _executive.SetTransactionContext(new TransactionContext
            {
                Transaction = setMiningNodes
            }).Apply();
            
            var getMiningNodes = new Transaction
            {
                From = Hash.Zero,
                To = Hash.Zero,
                MethodName = "GetMiningNodes",
                Params = ByteString.CopyFrom(new Parameters()
                {
                    Params = {
                        new Param
                        {
                            HashVal = Hash.Zero
                        }
                    }
                }.ToByteArray())
            };

            var miningNodes = new TransactionContext
            {
                Transaction = getMiningNodes
            };

            await _executive.SetTransactionContext(miningNodes).Apply();

            var nodes = miningNodes.Trace.RetVal?.Unpack<MiningNodes>().Nodes;
            
            Assert.NotNull(nodes);
        }
        
        [Fact]
        public async Task FirstTwoRoundsOrderTest()
        {
            
            var randomizeOrder = new Transaction
            {
                From = Hash.Zero,
                To = Hash.Zero,
                MethodName = "RandomizeOrderForFirstTwoRounds",
                Params = ByteString.CopyFrom(
                    new Parameters().ToByteArray()
                )
            };
            
            await _executive.SetTransactionContext(new TransactionContext
            {
                Transaction = randomizeOrder
            }).Apply();
        }
    }
}