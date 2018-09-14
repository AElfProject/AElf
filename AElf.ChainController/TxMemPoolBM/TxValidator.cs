using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.ChainController.TxMemPool;
using AElf.Common.ByteArrayHelpers;
using AElf.Configuration;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Managers;
using Easy.MessageHub;

namespace AElf.ChainController.TxMemPoolBM
{
    public class TxValidator : ITxValidator
    {
        private readonly ITxPoolConfig _config;
        private readonly IChainService _chainService;
        private IBlockChain _blockChain;
        private CanonicalBlockHashCache _canonicalBlockHashCache;

        private IBlockChain BlockChain
        {
            get
            {
                if (_blockChain == null)
                {
                    _blockChain =
                        _chainService.GetBlockChain(ByteArrayHelpers.FromHexString(NodeConfig.Instance.ChainId));
                }

                return _blockChain;
            }
        }

        public TxValidator(ITxPoolConfig config, IChainService chainService)
        {
            _config = config;
            _chainService = chainService;
            _canonicalBlockHashCache = new CanonicalBlockHashCache(BlockChain);
        }

        /// <summary>
        /// validate a tx size, signature, account format
        /// </summary>
        /// <param name="pool"></param>
        /// <param name="tx"></param>
        /// <returns></returns>
        public TxValidation.TxInsertionAndBroadcastingError ValidateTx(Transaction tx)
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

            if (!tx.CheckAccountAddress())
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

        public async Task<TxValidation.TxInsertionAndBroadcastingError> ValidateReferenceBlockAsync(Transaction tx)
        {
            if (tx.RefBlockNumber == 0 && Hash.Genesis.CheckPrefix(tx.RefBlockPrefix))
            {
                return TxValidation.TxInsertionAndBroadcastingError.Valid;
            }

            var curHeight = _canonicalBlockHashCache.CurrentHeight;
            if (tx.RefBlockNumber > curHeight)
            {
                return TxValidation.TxInsertionAndBroadcastingError.InvalidReferenceBlock;
            }

            if (curHeight > Globals.ReferenceBlockValidPeriod &&
                tx.RefBlockNumber < curHeight - Globals.ReferenceBlockValidPeriod)
            {
                return TxValidation.TxInsertionAndBroadcastingError.ExpiredReferenceBlock;
            }

            Hash canonicalHash;
            if (curHeight == 0)
            {
                canonicalHash = await BlockChain.GetCurrentBlockHashAsync();
            }
            else
            {
                canonicalHash = _canonicalBlockHashCache.GetHashByHeight(tx.RefBlockNumber);
            }
            if (canonicalHash == null)
            {
                throw new Exception($"Unable to get canonical hash for height {tx.RefBlockNumber}");
            }
            var res = canonicalHash.CheckPrefix(tx.RefBlockPrefix)
                ? TxValidation.TxInsertionAndBroadcastingError.Valid
                : TxValidation.TxInsertionAndBroadcastingError.InvalidReferenceBlock;
            return res;
        }
    }
}