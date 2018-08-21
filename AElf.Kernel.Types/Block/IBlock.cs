namespace AElf.Kernel
{
    public interface IBlock : IHashProvider
    {
        bool AddTransaction(Hash tx);
        BlockHeader Header { get; set; }
        BlockBody Body { get; set; }
        ulong RoundNumber { get; set; }
        void FillTxsMerkleTreeRootInHeader();
    }
}