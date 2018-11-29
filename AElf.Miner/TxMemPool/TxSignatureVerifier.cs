using AElf.Common;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;

namespace AElf.Miner.TxMemPool
{
    public class TxSignatureVerifier : ITxSignatureVerifier
    {
        public bool Verify(Transaction tx)
        {
            // todo warning adr recheck adr validity
            //            var addr = Address.FromRawBytes(pubKey);
            //            if (!addr.Equals(tx.From))
            //                return false;
            
            var verifier = new ECVerifier();
            return verifier.Verify(tx.GetSignature(), tx.GetHash().DumpByteArray());
        }
    }
}