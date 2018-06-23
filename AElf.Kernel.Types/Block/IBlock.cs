namespace AElf.Kernel.Types
{
    public interface IBlock : IHashProvider
    {
        bool AddTransaction(Hash tx);
        BlockHeader Header { get; set; }
        BlockBody Body { get; set; }
        void FillTxsMerkleTreeRootInHeader();
    }
}