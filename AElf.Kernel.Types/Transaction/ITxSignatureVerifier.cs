namespace AElf.Kernel.Types.Transaction
{
    public interface ITxSignatureVerifier
    {
        bool Verify(Kernel.Transaction tx);
    }
}