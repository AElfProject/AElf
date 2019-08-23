using System.Collections.Generic;

namespace AElf.WebApp.Application.Chain.Dto
{
    public class BlockBodyDto
    {
        public int TransactionsCount { get; set; }
        
        public List<string> Transactions { get; set; }
    }
}