using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.Common;
using AElf.Configuration;
using AElf.Configuration.Config.Chain;
using AElf.Kernel;
using AElf.Kernel.Managers;
using AElf.Miner.TxMemPool.RefBlockExceptions;
using Google.Protobuf;
using NLog;

namespace AElf.Miner.TxMemPool
{
    public class TxRefBlockValidator : ITxRefBlockValidator
    {
        private IChainService _chainService;
        private IBlockChain _blockChain;
        private CanonicalBlockHashCache _canonicalBlockHashCache;

        private IBlockChain BlockChain
        {
            get
            {
                if (_blockChain == null)
                {
                    _blockChain =
                        _chainService.GetBlockChain(Hash.LoadBase58(ChainConfig.Instance.ChainId));
                }

                return _blockChain;
            }
        }

        public TxRefBlockValidator(IChainService chainService)
        {
            try
            {
                _chainService = chainService;
                _canonicalBlockHashCache = new CanonicalBlockHashCache(BlockChain, LogManager.GetLogger(nameof(TxRefBlockValidator)));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public async Task ValidateAsync(Transaction tx)
        {
            if (tx.RefBlockNumber < GlobalConfig.GenesisBlockHeight && CheckPrefix(Hash.Genesis, tx.RefBlockPrefix))
            {
                return;
            }

            var curHeight = _canonicalBlockHashCache.CurrentHeight;
            if (tx.RefBlockNumber > curHeight && curHeight > GlobalConfig.GenesisBlockHeight)
            {
                throw  new FutureRefBlockException();
            }

            if (curHeight > GlobalConfig.ReferenceBlockValidPeriod + GlobalConfig.GenesisBlockHeight &&
                curHeight - tx.RefBlockNumber > GlobalConfig.ReferenceBlockValidPeriod)
            {
                throw new RefBlockExpiredException();
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
                canonicalHash = (await BlockChain.GetBlockByHeightAsync(tx.RefBlockNumber)).GetHash();
            }

            if (canonicalHash == null)
            {
                throw new Exception(
                    $"Unable to get canonical hash for height {tx.RefBlockNumber} - current height: {curHeight}");
            }

            // TODO: figure out why do we need this
            if (GlobalConfig.BlockProducerNumber == 1)
            {
                return;
            }

            if (CheckPrefix(canonicalHash, tx.RefBlockPrefix))
            {
                return;
            }
            throw  new RefBlockInvalidException();
        }

        private static bool CheckPrefix(Hash blockHash, ByteString prefix)
        {
            if (prefix.Length > blockHash.Value.Length)
            {
                return false;
            }

            return !prefix.Where((t, i) => t != blockHash.Value[i]).Any();
        }
    }
}