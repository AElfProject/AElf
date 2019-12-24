namespace AElf
{
    public interface IExecutionObserver
    {
        void Count();

        int GetUsage();
    }
}