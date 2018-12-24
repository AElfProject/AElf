using System;
using System.Linq;
using AElf.Common;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Types.Transaction
{
    public class TxSignatureVerifier : ITxSignatureVerifier, ITransientDependency
    {
        public bool Verify(Kernel.Transaction tx)
        {
            if (tx.Sigs == null || tx.Sigs.Count == 0)
            {
                return false;
            }

            if (tx.Sigs.Count == 1 && tx.Type != AElf.Kernel.TransactionType.MsigTransaction)
            {
                var pubkey =
                    CryptoHelpers.RecoverPublicKey(tx.Sigs.First().ToByteArray(), tx.GetHash().DumpByteArray());
                return Address.FromPublicKey(pubkey).Equals(tx.From);
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