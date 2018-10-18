using AElf.Common;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using GlobalConfig = AElf.Common.GlobalConfig;

namespace AElf.Miner.TxMemPool
{
    public static class TxValidation
    {
        public enum TxInsertionAndBroadcastingError
        {
            Success = 0,
            AlreadyInserted,
            Valid,
            WrongTransactionType,
            InvalidTxFormat,
            NotEnoughGas,
            TooBigSize,
            WrongAddress,
            InvalidSignature,
            PoolClosed,
            BroadCastFailed,
            Failed,
            AlreadyExecuted,
            InvalidReferenceBlock,
            ExpiredReferenceBlock,
            KnownTx
        }
        
        
        /// <summary>
        /// verify signature in tx
        /// </summary>
        /// <param name="tx"></param>
        /// <returns></returns>
        public static bool VerifySignature(this Transaction tx)
        {
            if (tx.P == null)
            {
                return false;
            }

            
            byte[] uncompressedPrivKey = tx.P.ToByteArray();
            var addr = Address.FromRawBytes(uncompressedPrivKey);
//            Hash addr = uncompressedPrivKey.Take(ECKeyPair.AddressLength).ToArray();

            if (!addr.Equals(tx.From))
                return false;
            ECKeyPair recipientKeyPair = ECKeyPair.FromPublicKey(uncompressedPrivKey);
            ECVerifier verifier = new ECVerifier(recipientKeyPair);
            return verifier.Verify(tx.GetSignature(), tx.GetHash().DumpByteArray());

        }

        
        /// <summary>
        /// verify address
        /// </summary>
        /// <param name="tx"></param>
        /// <returns></returns>
        public static bool CheckAccountAddress(this Transaction tx)
        {
            // TODO: more verifications
            return tx.From.Value.Length == GlobalConfig.AddressLength && (tx.To == null || tx.To.Value.Length == GlobalConfig.AddressLength);
        }
        
       
        
        /// <summary>
        /// return size of given tx
        /// </summary>
        /// <param name="tx"></param>
        /// <returns></returns>
        private static int GetTxSize(Transaction tx)
        {
            return tx.Size();
        }
    }
}
