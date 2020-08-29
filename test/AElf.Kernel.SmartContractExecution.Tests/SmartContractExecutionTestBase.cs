using AElf.TestBase;

namespace AElf.Kernel.SmartContractExecution
{
    public class SmartContractExecutionTestBase : AElfIntegratedTest<SmartContractExecutionTestAElfModule>
    {
        
    }
    
    public class SmartContractExecutionExecutingTestBase : AElfIntegratedTest<FullBlockChainExecutingTestModule>
    {
        
    }

    public class ValidateBeforeFailedTestBase : AElfIntegratedTest<ValidateBeforeFailedTestAElfModule>
    {
        
    }

    public class ValidateAfterFailedTestBase : AElfIntegratedTest<ValidateAfterFailedTestAElfModule>
    {
        
    }
}