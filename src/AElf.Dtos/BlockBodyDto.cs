using System.Collections.Generic;

namespace AElf.Dtos
{
    public class BlockBodyDto
    {
        public int TransactionsCount { get; set; }
        
        public List<string> Transactions { get; set; }
    }
}