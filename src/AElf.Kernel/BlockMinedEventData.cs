using System.Collections.Generic;

namespace AElf.Kernel;

public class BlockMinedEventData
{
    public BlockHeader BlockHeader { get; set; }
    public List<Transaction> Transactions { get; set; }
}