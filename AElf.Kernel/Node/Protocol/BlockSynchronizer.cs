using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common.ByteArrayHelpers;
using AElf.Kernel.BlockValidationFilters;
using AElf.Kernel.Miner;
using AElf.Kernel.Node.Protocol.Exceptions;
using AElf.Network.Peers;

[assembly: InternalsVisibleTo("AElf.Kernel.Tests")]
namespace AElf.Kernel.Node.Protocol
{
    // Initialization - On startup - For every peer get the height of the chain.
    // Distribute the work accordingly
    // We give everyone the current 
    
    public class BlockSynchedArgs : EventArgs
    {
        public Block Block { get; set; }
    }
    
    public class BlockSynchronizer
    {
        public event EventHandler BlockSynched;
        
        // React to new peers connected
        // when a peer connects get the height of his chain
        public IPeerManager _peerManager;
            
        private List<PendingBlock> PendingBlocks { get; }

        // The height of the chain 
        private int InitialHeight = 0;
        
        // The peers blockchain height with the highest height
        private int PeerHighestHight = 0;

        // The peer with the highest hight might not have all the blockchain
        // so the target height is PeerHighestHight + target.
        private int Security = 0;

        private Timer _cycleTimer;
        private IAElfNode _mainChainNode;

        public BlockSynchronizer(IAElfNode node)
        {
            PendingBlocks = new List<PendingBlock>();
            _mainChainNode = node;
            _cycleTimer = new Timer(DoCycle, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
        }

        public void SetPeerHeight()
        {
            
        }

        internal void DoCycle(object state)
        {
            
        }

        public void Start(Hash lastBlockHash)
        {
            // Start sync from block hash
        }

        /// <summary>
        /// When a block is received through the network it is placed here for sync
        /// purposes. Most of the time it will directly throw the <see cref="BlockSynched"/>
        /// event. In the case that the transaction was not received through the
        /// network, it will be placed here to sync.
        /// </summary>
        /// <param name="block"></param>
        public async Task AddBlockToSync(Block block)
        {
            if (block?.Header == null || block.Body == null)
                throw new InvalidBlockException("The block, blockheader or body is null");
            
            if (block.Body.Transactions == null || block.Body.Transactions.Count <= 0)
                throw new InvalidBlockException("The block contains no transactions");

            byte[] h = null;
            try
            {
                h = block.GetHash().GetHashBytes();
            }
            catch (Exception e)
            {
                throw new InvalidBlockException("Invalid block hash");
            }

            if (GetBlock(h) != null)
                return;

            List<Hash> missingTxs = _mainChainNode.GetMissingTransactions(block);

            if (missingTxs == null)
            {
                // todo what happend when the pool fails ?
                return;
            }

            if (missingTxs.Any())
            {
                // todo check that the returned txs are actually in the block
                PendingBlock newPendingBlock = new PendingBlock(h, block, missingTxs);
                PendingBlocks.Add(newPendingBlock);
            }
            else
            {
                // Here all the txs where in the pool, we try to add the block to
                // the chain
                BlockExecutionResult res = await _mainChainNode.AddBlock(block);
                
                if (res.ValidationError.HasValue && res.ValidationError.Value == ValidationError.OrphanBlock)
                {
                    // Here we've come across a block that is higher than the current
                    // chain height, we need to wait for it.
                    PendingBlock newPendingBlock = new PendingBlock(h, block, null);
                    newPendingBlock.IsWaitingForPrevious = true;
                    PendingBlocks.Add(newPendingBlock);
                }
            }
        }

        public void SetTransaction(byte[] blockHash, Transaction t)
        {
            PendingBlock b = GetBlock(blockHash);
            
        }

        public PendingBlock GetBlock(byte[] hash)
        {
            return PendingBlocks?.FirstOrDefault(p => p.BlockHash.BytesEqual(hash));
        }
    }

    public class PendingBlock
    {
        private Block _block;
        
        public List<byte[]> MissingTxs { get; private set; }

        public bool IsWaitingForPrevious = false;
            
        public byte[] BlockHash { get; }

        public PendingBlock(byte[] blockHash, Block block, List<Hash> missing)
        {
            _block = block;
            BlockHash = blockHash;
            
            MissingTxs = missing == null ? new List<byte[]>() : missing.Select(m => m.Value.ToByteArray()).ToList();
        }
        
        public void RemoveTransaction(byte[] txid)
        {
            
        }
    }
}