using AElf.Kernel;
using AElf.Kernel.SmartContract;
using AElf.TestBase;
using AElf.Types;
using Google.Protobuf;

namespace AElf.Runtime.WebAssembly.Tests;

public class WebAssemblyRuntimeTestBase : AElfIntegratedTest<WebAssemblyRuntimeTestAElfModule>
{
    internal TransactionContext MockTransactionContext(string functionName, ByteString? param = null)
    {
        var tx = new Transaction
        {
            From = SampleAddress.AddressList[0],
            To = SampleAddress.AddressList[1],
            MethodName = functionName,
            Params = param ?? ByteString.Empty
        };
        return new TransactionContext
        {
            Origin = SampleAddress.AddressList[0],
            Transaction = tx,
            Trace = new TransactionTrace()
        };
    }
}