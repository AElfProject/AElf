using System.Collections.Generic;

namespace AElf.Kernel.Concurrency.Scheduling
{
    public static class SchedulingHelpers
    {
        public static List<Hash> GetResources(this ITransaction tx)
        {
            return new List<Hash>(){
                tx.From, tx.To
            };
        }
    }
}
