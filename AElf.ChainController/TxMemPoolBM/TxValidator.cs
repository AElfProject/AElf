using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common.ByteArrayHelpers;
using AElf.Configuration;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using Akka.Cluster;

namespace AElf.ChainController.TxMemPool
{
    public class TxValidator : ITxValidator
    {
        private readonly ITxPoolConfig _config;
        private readonly IChainService _chainService;
        private IBlockChain _blockChain;

        private IBlockChain BlockChain
        {
            get
            {
                if (_blockChain == null)
                {
                    _blockChain = _chainService.GetBlockChain(ByteArrayHelpers.FromHexString(NodeConfig.Instance.ChainId));
                }

                return _blockChain;
            }
        }

        public TxValidator(ITxPoolConfig config, IChainService chainService)
        {
            _config = config;
            _chainService = chainService;
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
            var bc = BlockChain;
            var curHeight = await bc.GetCurrentBlockHeightAsync();
            if (tx.RefBlockNumber > curHeight)
            {
                return TxValidation.TxInsertionAndBroadcastingError.InvalidReferenceBlock;
            }

            if (curHeight > Globals.ReferenceBlockValidPeriod && tx.RefBlockNumber < curHeight - Globals.ReferenceBlockValidPeriod)
            {
                return TxValidation.TxInsertionAndBroadcastingError.ExpiredReferenceBlock;
            }

            var canonicalHash = curHeight == 0
                ? await bc.GetCurrentBlockHashAsync()
                : await bc.GetCanonicalHashAsync(tx.RefBlockNumber);
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