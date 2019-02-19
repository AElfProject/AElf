using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.Kernel.TransactionPool.RefBlockExceptions;
using Google.Protobuf;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.TransactionPool
{
    public class TxRefBlockValidator : ITxRefBlockValidator, ISingletonDependency
    {
        private IBlockchainService _blockchainService;

        public TxRefBlockValidator(IBlockchainService blockchainService)
        {
            _blockchainService = blockchainService;
        }

        public async Task ValidateAsync(int chainId, Transaction tx)
        {

            if (tx.RefBlockNumber < GlobalConfig.GenesisBlockHeight && CheckPrefix(Hash.Genesis, tx.RefBlockPrefix))
            {
                return;
            }

            var chain = await _blockchainService.GetChainAsync(chainId);

            var canonicalHash = await _blockchainService.GetBlockHashByHeightAsync(chain, tx.RefBlockNumber);

            var curHeight = chain.BestChainHeight;
            if (tx.RefBlockNumber > curHeight && curHeight > GlobalConfig.GenesisBlockHeight)
            {
                throw  new FutureRefBlockException();
            }

            if (curHeight > GlobalConfig.ReferenceBlockValidPeriod + GlobalConfig.GenesisBlockHeight &&
                curHeight - tx.RefBlockNumber > GlobalConfig.ReferenceBlockValidPeriod)
            {
                throw new RefBlockExpiredException();
            }

            if (canonicalHash == null)
            {
                throw new Exception(
                    $"Unable to get canonical hash for height {tx.RefBlockNumber} - current height: {curHeight}");
            }

            if (CheckPrefix(canonicalHash, tx.RefBlockPrefix))
            {
                return;
            }
            throw new RefBlockInvalidException();
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