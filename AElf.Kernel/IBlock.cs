namespace AElf.Kernel
{
    public interface IBlock : IHashProvider
    {
        Hash GetHash();
        bool AddTransaction(Hash tx);
        BlockHeader Header { get; set; }
        BlockBody Body { get; set; }
        void FillTxsMerkleTreeRootInHeader();
    }
}