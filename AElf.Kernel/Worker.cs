using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QuickGraph;

namespace AElf.Kernel
{
    /// <summary>
    /// Define the worker to proces tx
    /// just demo for test
    /// </summary>
    public class Worker: IWorker
    {
        public static Dictionary<IHash, int> ExecutePlan = new Dictionary<IHash, int>();
        private object obj = new object();

        public void process(IHash hash, int phase)
        {
            //processing demo
            lock (obj)
            {
                ExecutePlan[hash] = phase;
            }
        }
    }
}