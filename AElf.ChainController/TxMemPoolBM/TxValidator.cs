using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;

namespace AElf.ChainController.TxMemPool
{
    public class TxValidator
    {
        private readonly ITxPoolConfig _config;
        private readonly IBlockChain _blockChain;

        public TxValidator(ITxPoolConfig config, IBlockChain blockChain)
        {
            _config = config;
            _blockChain = blockChain;
        }

        /// <summary>
        /// validate a tx size, signature, account format
        /// </summary>
        /// <param name="pool"></param>
        /// <param name="tx"></param>
        /// <returns></returns>
        public TxValidation.TxInsertionAndBroadcastingError ValidateTx(ITransaction tx)
        {
            // Basically the same as TxValidation.ValidateTx but without TransactionType
            if (tx.From == Hash.Zero || tx.MethodName == "")
            {
                return TxValidation.TxInsertionAndBroadcastingError.InvalidTxFormat;
            }
            
            // size validation
            if (tx.Size() > _config.TxLimitSize)
            {
                return TxValidation.TxInsertionAndBroadcastingError.TooBigSize;
            }
            
            // TODO: signature validation
            if (!tx.VerifySignature())
            {
                return TxValidation.TxInsertionAndBroadcastingError.InvalidSignature;
            }
            
            if(!tx.CheckAccountAddress())
            {
                return TxValidation.TxInsertionAndBroadcastingError.WrongAddress;
            }

            // TODO: check block reference

            /*// fee validation
            if (tx.Fee < pool.MinimalFee)
            {
                // TODO: log errors, not enough Fee error 
                return false;
            }*/
            
            // TODO : more validations
            return TxValidation.TxInsertionAndBroadcastingError.Valid;
        }

        public async Task<TxValidation.TxInsertionAndBroadcastingError> CheckReferenceBlockAsync(ITransaction tx)
        {
            var curHeight = await _blockChain.GetCurrentBlockHeightAsync();
            if (tx.RefBlockNumber > curHeight)
            {
                return TxValidation.TxInsertionAndBroadcastingError.InvalidReferenceBlock;
            }
            if(tx.RefBlockNumber < curHeight - 64)
            {
                return TxValidation.TxInsertionAndBroadcastingError.ExpiredReferenceBlock;
            }

            var canonicalHash = await _blockChain.GetCanonicalHashAsync(tx.RefBlockNumber);
            if (canonicalHash == null)
            {
                throw new Exception($"Unable to get canonical hash for height {tx.RefBlockNumber}");
            }

            return canonicalHash.CheckPrefix(tx.RefBlockPrefix)
                ? TxValidation.TxInsertionAndBroadcastingError.Valid
                : TxValidation.TxInsertionAndBroadcastingError.InvalidReferenceBlock;
        }
    }
}
