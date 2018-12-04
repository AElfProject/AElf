using AElf.Common;
using AElf.Cryptography.ECDSA;
using Google.Protobuf;

namespace AElf.Kernel.Types.Transaction
{
    public class TxSignatureVerifier : ITxSignatureVerifier
    {
        public bool Verify(Kernel.Transaction tx)
        {
            if (tx.Sigs == null || tx.Sigs.Count == 0)
            {
                return false;
            }

            if (tx.Sigs.Count == 1 && tx.Type != TransactionType.MsigTransaction)
            {
                // todo Check the address of signer if only one signer.
//                var pubKey = tx.Sigs[0].P.ToByteArray();
//                var addr = Address.FromRawBytes(pubKey);
//
//                if (!addr.Equals(tx.From))
//                    return false;
            }
            
            foreach (var sig in tx.Sigs)
            {
                var verifier = new ECVerifier();
                
                if(verifier.Verify(new ECSignature(sig.ToByteArray()), tx.GetHash().DumpByteArray()))
                    continue;
                
                return false;
            }
            return true;
        }
    }
}