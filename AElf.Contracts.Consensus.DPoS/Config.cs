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

        public static int InitialWaitingMilliseconds = 4000;

        public static ulong GetDividendsForEveryMiner(ulong minedBlocks)
        {
            return (ulong) (minedBlocks * DPoSContractConsts.ElfTokenPerBlock * DPoSContractConsts.MinersBasicRatio /
                            GetProducerNumber());
        }

        public static ulong GetDividendsForTicketsCount(ulong minedBlocks)
        {
            return (ulong) (minedBlocks * DPoSContractConsts.ElfTokenPerBlock * DPoSContractConsts.MinersVotesRatio);
        }
        
        public static ulong GetDividendsForReappointment(ulong minedBlocks)
        {
            return (ulong) (minedBlocks * DPoSContractConsts.ElfTokenPerBlock * DPoSContractConsts.MinersReappointmentRatio);
        }
        
        public static ulong GetDividendsForBackupNodes(ulong minedBlocks)
        {
            return (ulong) (minedBlocks * DPoSContractConsts.ElfTokenPerBlock * DPoSContractConsts.BackupNodesRatio);
        }

        public static ulong GetDividendsForVoters(ulong minedBlocks)
        {
            return (ulong) (minedBlocks * DPoSContractConsts.ElfTokenPerBlock * DPoSContractConsts.VotersRatio);
        }
        
        public static ulong GetDividendsForAll(ulong minedBlocks)
        {
            return minedBlocks * DPoSContractConsts.ElfTokenPerBlock;
        }

        public static int GetProducerNumber()
        {
            return 17 + (DateTime.UtcNow.Year - 2019) * 2;
        }
    }
}