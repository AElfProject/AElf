using System.Collections.Generic;
using AElf.TestBase;

namespace AElf.Kernel;

public class AElfKernelTestBase : AElfIntegratedTest<KernelCoreTestAElfModule>
{
    protected const string AElfBlockchainModule = nameof(AElfBlockchainModule);

    protected static string GetBlockExecutedDataKey<T>(IMessage key = null)
    {
        var list = new List<string> { KernelConstants.BlockExecutedDataKey, typeof(T).Name };
        if (key != null) list.Add(key.ToString());
        return string.Join("/", list);
    }
}

public class AElfKernelWithChainTestBase : AElfIntegratedTest<KernelCoreWithChainTestAElfModule>
{
    protected const string AElfBlockchainModule = nameof(AElfBlockchainModule);
}

public class AElfMinerTestBase : AElfIntegratedTest<KernelMinerTestAElfModule>
{
    protected const string AElfMinerModule = nameof(AElfMinerModule);
}

public class AccountTestBase : AElfIntegratedTest<AccountTestAElfModule>
{
    protected const string AElfAccountModule = nameof(AElfAccountModule);
}