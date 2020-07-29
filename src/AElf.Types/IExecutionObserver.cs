namespace AElf
{
    public interface IExecutionObserver
    {
        void BranchCount();

        void CallCount();

        int GetCallCount();
        
        int GetBranchCount();
    }
}