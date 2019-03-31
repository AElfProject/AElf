using System.Collections.Generic;

namespace AElf.WebApp.Application.Chain.Dto
{
    public class BlockStateDto
    {
        public string BlockHash { get; set; }
        
        public string PreviousHash { get; set; }
        
        public long BlockHeight { get; set; }
        
        public Dictionary<string,string > Changes { get; set; }
    }
}