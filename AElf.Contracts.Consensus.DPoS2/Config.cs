using System;
using System.Collections.Generic;
using AElf.Common;

namespace AElf.Contracts.Consensus.DPoS
{
    public static class Config
    {
        public static List<string> InitialMinersAliases => new List<string>
        {
            "YQ", "SM", "WK", "CP", "PG", 
            "SC", "ZX", "ZY", "YS", "MH", 
            "ZZ", "ZA", "GL", "LN", "DW",
            "BB", "MM", "DZ", "JJ", "DD"
        };

        public static int InitialWaitingMilliseconds = 8000;

        public static ulong GetDividendsForEveryMiner(ulong minedBlocks)
        {
            return (ulong) (minedBlocks * DividendsSettings.ElfTokenPerBlock * DividendsSettings.MinersBasicRatio /
                            GetProducerNumber());
        }

        public static ulong GetDividendsForTicketsCount(ulong minedBlocks)
        {
            return (ulong) (minedBlocks * DividendsSettings.ElfTokenPerBlock * DividendsSettings.MinersVotesRatio);
        }
        
        public static ulong GetDividendsForReappointment(ulong minedBlocks)
        {
            return (ulong) (minedBlocks * DividendsSettings.ElfTokenPerBlock * DividendsSettings.MinersReappointmentRatio);
        }
        
        public static ulong GetDividendsForBackupNodes(ulong minedBlocks)
        {
            return (ulong) (minedBlocks * DividendsSettings.ElfTokenPerBlock * DividendsSettings.BackupNodesRatio);
        }

        public static ulong GetDividendsForVoters(ulong minedBlocks)
        {
            return (ulong) (minedBlocks * DividendsSettings.ElfTokenPerBlock * DividendsSettings.VotersRatio);
        }
        
        public static ulong GetDividendsForAll(ulong minedBlocks)
        {
            return minedBlocks * DividendsSettings.ElfTokenPerBlock;
        }

        public static int GetProducerNumber()
        {
            return 17 + (DateTime.UtcNow.Year - 2019) * 2;
        }
    }
}