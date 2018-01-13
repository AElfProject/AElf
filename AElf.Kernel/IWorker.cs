using QuickGraph;

namespace AElf.Kernel
{
    /// <summary>
    /// Worker to process transaction
    /// </summary>
    public interface IWorker
    {
        void process(IHash hash, int phase);
    }
}