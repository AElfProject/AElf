using System.Linq;
using AElf.TestBase;
using AElf.Types;

namespace AElf.Kernel.CodeCheck.Tests;

public class CodeCheckTestBase : AElfIntegratedTest<CodeCheckTestAElfModule>
{
    public static Address NormalAddress = SampleAddress.AddressList.Last();
    public static Address ParliamentContractFakeAddress = SampleAddress.AddressList.First();
    public static Address ZeroContractFakeAddress = SampleAddress.AddressList.First();
}

public class CodeCheckParallelTestBase : AElfIntegratedTest<CodeCheckParallelTestAElfModule>
{
    public static Address NormalAddress = SampleAddress.AddressList.Last();
    public static Address ZeroContractFakeAddress = SampleAddress.AddressList.First();
}