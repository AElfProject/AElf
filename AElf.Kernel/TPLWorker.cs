using System;
namespace AElf.Kernel
{
    /// <summary>
    /// The embeded TPL based worker.
    /// </summary>
    public class TPLWorker:IWorker
    {
        public TPLWorker()
        {
        }

        public void process(IHash hash, int phase)
        {
            throw new NotImplementedException();
        }
    }
}
