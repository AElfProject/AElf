using AElf.Common;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;

namespace AElf.Miner.TxMemPool
{
    public class TxSignatureVerifier : ITxSignatureVerifier
    {
        public bool Verify(Transaction tx)
        {
            if (tx.P == null)
            {
                return false;
            }

            var pubKey = tx.P.ToByteArray();
            var addr = Address.FromRawBytes(pubKey);

            if (!addr.Equals(tx.From))
                return false;
            var keyPair = ECKeyPair.FromPublicKey(pubKey);
            var verifier = new ECVerifier(keyPair);
            return verifier.Verify(tx.GetSignature(), tx.GetHash().DumpByteArray());
        }
    }
}