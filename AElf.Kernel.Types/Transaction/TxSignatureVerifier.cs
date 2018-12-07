using System;
using System.Linq;
using AElf.Common;
using AElf.Cryptography;

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
                var pubkey =
                    CryptoHelpers.RecoverPublicKey(tx.Sigs.First().ToByteArray(), tx.GetHash().DumpByteArray());
                return Address.FromPublicKey(new byte[0], pubkey) == tx.From;
            }

            // Multi sig, TODO old logic tries to recover pubkey here (do we really need)

            return true;
        }
    }
}