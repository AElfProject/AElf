using System.Threading.Tasks;
using AElf.ChainController;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Managers;
using AElf.Miner.TxMemPool;
using AElf.Synchronization.BlockSynchronization;
using Google.Protobuf.Collections;

namespace AElf.Node
{
    public class P2PHandler
    {
        public IChainService ChainService { get; set; }
        public IBlockSynchronizer BlockSynchronizer { get; set; }
        public ITxHub TxHub { get; set; }
        public ITransactionManager TransactionManager { get; set; }

        public async Task<Block> GetBlockAtHeight(int height)
        {
            //var blockchain = ChainService.GetBlockChain(Hash.LoadHex(NodeConfig.Instance.ChainId));
            //return (Block) await blockchain.GetBlockByHeightAsync((ulong) height);

            var block = (Block) await ChainService.GetBlockChain(Hash.Default).GetBlockByHeightAsync((ulong)height);
            return block != null ? await FillBlockWithTransactionList(block) : null;
        }
        
        public async Task<Block> GetBlockFromHash(Hash hash)
        {
            var block = await Task.Run(() => (Block) BlockSynchronizer.GetBlockByHash(hash));
            return block != null ? await FillBlockWithTransactionList(block) : null;
        }

        public async Task<BlockHeaderList> GetBlockHeaderList(ulong index, int count)
        {
            return await BlockSynchronizer.GetBlockHeaderList(index, count);
        }

        public async Task<Transaction> GetTransaction(Hash txId)
        {
            if (TxHub.TryGetTx(txId, out var tx))
            {
                return tx;
            }

            return await TransactionManager.GetTransaction(txId);
        }

        private async Task<Block> FillBlockWithTransactionList(Block block)
        {
            block.Body.TransactionList.Clear();
            foreach (var txId in block.Body.Transactions)
            {
                var r = await TxHub.GetReceiptAsync(txId);
                block.Body.TransactionList.Add(r.Transaction);
            }

            return block;
        }
    }
}