using AElf.Kernel;

namespace AElf.Miner.TxMemPool
{
    public interface ITxSignatureVerifier
    {
        bool Verify(Transaction tx);
    }
}