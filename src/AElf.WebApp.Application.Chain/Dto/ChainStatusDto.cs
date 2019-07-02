using System.Collections.Generic;

namespace AElf.WebApp.Application.Chain.Dto
{
    public class ChainStatusDto
    {
        public string ChainId { get; set; }
        
        public Dictionary<string,long> Branches { get; set; }
        
        public Dictionary<string,string>  NotLinkedBlocks { get; set; }
        
        public long LongestChainHeight { get; set; }
        
        public string LongestChainHash { get; set; }

        public string GenesisBlockHash { get; set; }
        
        public string GenesisContractAddress { get; set; }
        
        public string LastIrreversibleBlockHash { get; set; }
        
        public long LastIrreversibleBlockHeight { get; set; }
        
        public string BestChainHash { get; set; }
        
        public long BestChainHeight { get; set; }
    }
}