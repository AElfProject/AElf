using AElf.TestBase;

namespace AElf.Kernel.SmartContractExecution
{
    public class SmartContractExecutionTestBase : AElfIntegratedTest<SmartContractExecutionTestAElfModule>
    {
        
    }
    
    public class SmartContractExecutionExecutingTestBase : AElfIntegratedTest<FullBlockChainExecutingTestModule>
    {
        
    }

    public class ExecuteFailedTestBase : AElfIntegratedTest<ExecuteFailedTestAElfModule>
    {
        
    }

    public class ValidateAfterFailedTestBase : AElfIntegratedTest<ValidateAfterFailedTestAElfModule>
    {
        
    }
}