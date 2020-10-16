using System.Collections.Generic;
using System.Linq;
using AElf.Kernel;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.TestBase;
using AElf.Types;

namespace AElf.Sdk.CSharp.Tests
{
    public class SdkCSharpTestBase:AElfIntegratedTest<TestSdkCSharpAElfModule>
    {
        protected SdkCSharpTestBase()
        {
            StateProvider = GetRequiredService<IStateProviderFactory>().CreateStateProvider();
            BridgeContext = GetRequiredService<IHostSmartContractBridgeContextService>().Create();
            
            var transactionContextFactory = GetRequiredService<ITransactionContextFactory>();
            TransactionContext = transactionContextFactory.Create(new Transaction
            {
                From = AddressList[1],
                To = AddressList[0],
                MethodName = "Test"
            }, new ChainContext
            {
                BlockHash = HashHelper.ComputeFrom("PreviousBlockHash"),
                BlockHeight = 1
            });
        }

        private IStateProvider StateProvider { get; }
        protected IHostSmartContractBridgeContext BridgeContext { get; }
        protected ITransactionContext TransactionContext { get; }
        protected List<Address> AddressList { get; } = SampleAddress.AddressList.ToList();

    }
}