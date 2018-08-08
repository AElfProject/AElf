namespace AElf.Kernel.Types
{
    public enum ConsensusType
    {
        PoW = 0,
        // ReSharper disable once InconsistentNaming
        AElfDPoS = 1,
        // ReSharper disable once InconsistentNaming
        PoTC = 2,//Proof of Transaction Count. Used for testing execution performance of single node.
        // ReSharper disable once InconsistentNaming
        SingleNode = 3
    }
}