namespace AElf.Kernel.Worker
{
    /// <summary>
    /// Worker to process transaction
    /// </summary>
    public interface IWorker
    {
        void Process();
    }
}