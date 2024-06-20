namespace AElf.Kernel.Blockchain;

public class BlockExecutedSet
{
    public Block Block { get; set; }
    public IDictionary<Hash, TransactionResult> TransactionResultMap { get; set; }
    public List<TransactionResult> TransactionResults { get; set; }
    public List<Transaction> Transactions { get; set; }
    public long Height => Block.Height;

    public IEnumerable<Hash> TransactionIds => Block.TransactionIds;

    public Hash GetHash()
    {
        return Block.GetHash();
    }
}