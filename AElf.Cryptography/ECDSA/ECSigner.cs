namespace AElf.Cryptography.ECDSA
{
    public class ECSigner
    {
        public ECSignature Sign(ECKeyPair keyPair, byte[] data)
        {
            return new ECSignature(CryptoHelpers.SignWithPrivateKey(keyPair.PrivateKey, data));
        }
    }
}