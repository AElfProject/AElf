namespace AElf.Cryptography.ECDSA
{
    public class KeyPairGenerator
    {
        public ECKeyPair Generate()
        {
            // TODO: Use CryptoHelpers method directly, now keeping this interface for legacy
            return CryptoHelpers.GenerateKeyPair();
        }
    }
}