using System.Collections.Generic;
using AElf.Common;

namespace AElf.Contracts.Consensus.DPoS
{
    public static class Config
    {
        public static List<string> Aliases => new List<string>
        {
            "YQ", "SM", "WK", "CP", "PG", 
            "SC", "ZX", "ZY", "YS", "MH", 
            "ZZ", "ZA", "GL", "LN", "DW",
            "BB", "MM", "DZ", "JJ", "DD"
        };

        public static int InitialWaitingMilliseconds = 4000;

        public static ulong GetDividendsForEveryMiner(ulong minedBlocks)
        {
            return (ulong) (minedBlocks * GlobalConfig.ElfTokenPerBlock * GlobalConfig.DividendsForEveryMinerRatio /
                            GlobalConfig.BlockProducerNumber);
        }

        public static ulong GetDividendsForTicketsCount(ulong minedBlocks)
        {
            return (ulong) (minedBlocks * GlobalConfig.ElfTokenPerBlock * GlobalConfig.DividendsForTicketsCountRatio);
        }
        
        public static ulong GetDividendsForReappointment(ulong minedBlocks)
        {
            return (ulong) (minedBlocks * GlobalConfig.ElfTokenPerBlock * GlobalConfig.DividendsForReappointmentRatio);
        }
        
        public static ulong GetDividendsForBackupNodes(ulong minedBlocks)
        {
            return (ulong) (minedBlocks * GlobalConfig.ElfTokenPerBlock * GlobalConfig.DividendsForBackupNodesRatio);
        }

        public static ulong GetDividendsForVoters(ulong minedBlocks)
        {
            return (ulong) (minedBlocks * GlobalConfig.ElfTokenPerBlock * GlobalConfig.DividendsForVotersRatio);
        }
        
        public static ulong GetDividendsForAll(ulong minedBlocks)
        {
            return minedBlocks * GlobalConfig.ElfTokenPerBlock;
        }

        public const int DaysEachTerm = 7;
    }
}