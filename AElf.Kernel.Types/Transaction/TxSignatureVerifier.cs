using System;
using System.Linq;
using Secp256k1Net;

namespace AElf.Kernel.Types.Transaction
{
    public class TxSignatureVerifier : ITxSignatureVerifier, IDisposable
    {
        private readonly Secp256k1 _secp256k1 = new Secp256k1();

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

            foreach (var compactSig in tx.Sigs)
            {
                var recSig = new byte[Secp256k1.UNSERIALIZED_SIGNATURE_SIZE];
                _secp256k1.RecoverableSignatureParseCompact(recSig, compactSig.ToByteArray(), compactSig.Last());

                if (Verify(recSig, tx.GetHash().DumpByteArray()))
                    continue;

                return false;
            }

            return true;
        }

        private bool Verify(byte[] recoverableSig, byte[] hash)
        {
            if (recoverableSig == null || hash == null)
                return false;

            // recover
            byte[] publicKeyOutput = new byte[Secp256k1.PUBKEY_LENGTH];

            _secp256k1.Recover(publicKeyOutput, recoverableSig, hash);
            // TODO: No need to verify again
            return _secp256k1.Verify(recoverableSig, hash, publicKeyOutput);
        }

        public void Dispose()
        {
            _secp256k1.Dispose();
        }
    }
}