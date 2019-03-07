using System;
using System.Collections.Generic;

namespace AElf.Contracts.Consensus.DPoS
{
    public static class Config
    {
        public static int GetProducerNumber()
        {
            return 17 + (DateTime.UtcNow.Year - 2019) * 2;
        }
    }
}