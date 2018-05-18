using System.Collections.Generic;

namespace AElf.Kernel.Concurrency
{
    public static class ResourceExtractor
    {
        public static List<Hash> GetResources(this Transaction tx)
        {
            return new List<Hash>(){
                tx.From, tx.To
            };
        }
    }
}
