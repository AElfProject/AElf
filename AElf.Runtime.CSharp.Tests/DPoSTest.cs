using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Services;
using Google.Protobuf;
using Xunit.Frameworks.Autofac;

namespace AElf.Runtime.CSharp.Tests
{
    [UseAutofacTestFramework]
    // ReSharper disable once InconsistentNaming
    public class DPoSTest
    {
        private MockSetup _mock;
        private ISmartContractService _service;
        public DPoSTest(MockSetup mock)
        {
            _mock = mock;
            _service = mock.SmartContractService;
        }

        public async Task FirstTwoRoundsOrderTest()
        {
            var executive = await _service.GetExecutiveAsync(Hash.Zero, _mock.ChainId1);
            executive.SetSmartContractContext(new SmartContractContext()
            {
                ChainId = Hash.Zero,
                ContractAddress = Hash.Zero,
                DataProvider = _mock.DataProvider1.GetDataProvider(),
                SmartContractService = _service
            });
            
            var randomizeOrder = new Transaction
            {
                From = Hash.Zero,
                To = Hash.Zero,
                MethodName = "RandomizeOrderForFirstTwoRounds",
                Params = ByteString.CopyFrom(
                    new Parameters().ToByteArray()
                )
            };
            
            await executive.SetTransactionContext(new TransactionContext
            {
                Transaction = randomizeOrder
            }).Apply();
        }
    }
}