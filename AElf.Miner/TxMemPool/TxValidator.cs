using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.Common;
using AElf.Configuration;
using AElf.Configuration.Config.Chain;
using AElf.Kernel;
using AElf.Kernel.Managers;
using AElf.Types.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using NLog;

namespace AElf.Miner.TxMemPool
{
    public class TxValidator : ITxValidator
    {
        private readonly ITxPoolConfig _config;
        private readonly IChainService _chainService;
        private IBlockChain _blockChain;
        private readonly ILogger _logger;

        private readonly CanonicalBlockHashCache _canonicalBlockHashCache;

        
        private IBlockChain BlockChain
        {
            get
            {
                if (_blockChain == null)
                {
                    _blockChain =
                        _chainService.GetBlockChain(Hash.LoadHex(ChainConfig.Instance.ChainId));
                }

                return _blockChain;
            }
        }

        public TxValidator(ITxPoolConfig config, IChainService chainService, ILogger logger)
        {
            _config = config;
            _chainService = chainService;
            _logger = logger;
            _canonicalBlockHashCache = new CanonicalBlockHashCache(BlockChain, logger);
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
            if (tx.From == Address.Zero || tx.MethodName == "")
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

        private bool CheckPrefix(Hash blockHash, ByteString prefix)
        {
            if (prefix.Length > blockHash.Value.Length)
            {
                return false;
            }

            return !prefix.Where((t, i) => t != blockHash.Value[i]).Any();
        }
        
        public async Task<TxValidation.TxInsertionAndBroadcastingError> ValidateReferenceBlockAsync(Transaction tx)
        {
            if (tx.RefBlockNumber == 0 && CheckPrefix(Hash.Genesis, tx.RefBlockPrefix))
            {
                return TxValidation.TxInsertionAndBroadcastingError.Valid;
            }

            var curHeight = _canonicalBlockHashCache.CurrentHeight;
            if (tx.RefBlockNumber > curHeight && curHeight != 0)
            {
                return TxValidation.TxInsertionAndBroadcastingError.InvalidReferenceBlock;
            }

            if (curHeight > GlobalConfig.ReferenceBlockValidPeriod &&
                tx.RefBlockNumber < curHeight - GlobalConfig.ReferenceBlockValidPeriod)
            {
                return TxValidation.TxInsertionAndBroadcastingError.ExpiredReferenceBlock;
            }

            Hash canonicalHash;
            if (curHeight == 0)
            {
                canonicalHash = await BlockChain.GetCurrentBlockHashAsync();
                _logger?.Trace("Current block hash: " + canonicalHash.DumpHex());
            }
            else
            {
                canonicalHash = _canonicalBlockHashCache.GetHashByHeight(tx.RefBlockNumber);
            }

            if (canonicalHash == null)
            {
                canonicalHash = (await BlockChain.GetBlockByHeightAsync(tx.RefBlockNumber)).GetHash();
            }

            if (canonicalHash == null)
            {
                throw new Exception(
                    $"Unable to get canonical hash for height {tx.RefBlockNumber} - current height: {curHeight}");
            }

            if (GlobalConfig.BlockProducerNumber == 1)
            {
                return TxValidation.TxInsertionAndBroadcastingError.Valid;
            }

            var res = CheckPrefix(canonicalHash, tx.RefBlockPrefix)
                ? TxValidation.TxInsertionAndBroadcastingError.Valid
                : TxValidation.TxInsertionAndBroadcastingError.InvalidReferenceBlock;
            return res;
        }
    }
}