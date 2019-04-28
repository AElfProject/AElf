using AElf.Kernel;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Sdk;
using Xunit;
using CustomContract = AElf.Runtime.CSharp.Tests.TestContract;

namespace AElf.Sdk.CSharp.Tests
{
    public class CSharpSmartContractContextTests : SdkCSharpTestBase
    {
        private CustomContract.TestContract Contract = new CustomContract.TestContract();
        private IStateProvider StateProvider { get; }
        private IHostSmartContractBridgeContext BridgeContext { get; }
        private CSharpSmartContractContext ContractContext { get; }

        public CSharpSmartContractContextTests()
        {
            StateProvider = GetRequiredService<IStateProviderFactory>().CreateStateProvider();
            BridgeContext = GetRequiredService<IHostSmartContractBridgeContextService>().Create();
            
            var transactionContext = new TransactionContext()
            {
                Transaction = new Transaction()
                {
                    From = Address.Generate(),
                    To = Address.Generate()
                }
            };

            BridgeContext.TransactionContext = transactionContext;
            Contract.InternalInitialize(BridgeContext);
            
            ContractContext = new CSharpSmartContractContext(BridgeContext);
        }

        [Fact]
        public void TestPreviousBlockHash()
        {
            var hash = ContractContext.PreviousBlockHash;
        }
        
    }
}