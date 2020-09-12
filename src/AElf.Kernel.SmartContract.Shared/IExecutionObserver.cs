namespace AElf.Kernel.SmartContract
{
    public interface IExecutionObserver
    {
        void BranchCount();

        void CallCount();

        int GetCallCount();
        
        int GetBranchCount();
    }
}