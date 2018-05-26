using System;
using System.Collections.Generic;

namespace AElf.Kernel.Concurrency.Scheduling
{
    public static class SchedulingHelpers
    {
        public static List<Hash> GetResources(this Transaction tx)
        {
            return new List<Hash>(){
                tx.From, tx.To
            };
        }

        public static List<Hash> GetPathSet(this Transaction tx)
        {
            throw new NotImplementedException();
        }
        
        //TODO: need some way to determine whether it contains a ReadWriteAccountSharing data
    }
}
