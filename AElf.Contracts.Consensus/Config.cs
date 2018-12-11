using System.Collections.Generic;
using AElf.Common;

namespace AElf.Contracts.Consensus
{
    public static class Config
    {
        public static List<string> Aliases => new List<string>
        {
            "YQ", "SM", "WK", "CP", "PG", 
            "SC", "ZX", "ZY", "YS", "MH", 
            "ZZ", "ZA", "GL", "LN"
        };

        public static ulong GetDividendsForEveryMiner(ulong minedBlocks)
        {
            return (ulong) (minedBlocks * GlobalConfig.ElfTokenPerBlock * GlobalConfig.DividendsForEveryMiner /
                            GlobalConfig.BlockProducerNumber);
        }

        public static ulong GetDividendsForTicketsCount(ulong minedBlocks)
        {
            return (ulong) (minedBlocks * GlobalConfig.ElfTokenPerBlock * GlobalConfig.DividendsForTicketsCount /
                            GlobalConfig.BlockProducerNumber);
        }
        
        public static ulong GetDividendsForReappointment(ulong minedBlocks)
        {
            return (ulong) (minedBlocks * GlobalConfig.ElfTokenPerBlock * GlobalConfig.DividendsForReappointment /
                            GlobalConfig.BlockProducerNumber);
        }
        
        public static ulong GetDividendsForBackupNodes(ulong minedBlocks)
        {
            return (ulong) (minedBlocks * GlobalConfig.ElfTokenPerBlock * GlobalConfig.DividendsForBackupNodes /
                            GlobalConfig.BlockProducerNumber);
        }

        public static ulong GetDividendsForVoters(ulong minedBlocks)
        {
            return (ulong) (minedBlocks * GlobalConfig.ElfTokenPerBlock * GlobalConfig.DividendsForVoters /
                            GlobalConfig.BlockProducerNumber);
        }
    }
}