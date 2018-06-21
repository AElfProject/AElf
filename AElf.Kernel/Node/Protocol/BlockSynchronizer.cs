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
using AElf.Network.Data;
using AElf.Network.Peers;
using Google.Protobuf;

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
    
    /// <summary>
    /// When a node starts it creates this BlockSynchroniser for two reasons: the first
    /// is that the node is very probably behind other nodes on the network and it needs
    /// to perform an initial download of the already processed blocks; the second reason
    /// this is needed is that when the node receives a block, it's possible that some of 
    /// the transactions are not in the pool. In the later case, the block is placed here 
    /// and requests are sent out to retrieve missing transactions. These two operation are
    /// possibly performed at the same time, even though the Initial sync will at one point
    /// stop because we'll be receiving the new blocks through the network.
    /// </summary>
    public class BlockSynchronizer
    {
        public event EventHandler BlockSynched;
        
        // React to new peers connected
        // when a peer connects get the height of his chain
        public IPeerManager _peerManager;
            
        private List<PendingBlock> PendingBlocks { get; }

        private bool IsInitialSync { get; set; } = true;

        // The height of the chain, this gets incremented each time a 
        // block is successfully removed.
        public int CurrentHeight { get; private set; } = 0;
        
        // The peer with the highest hight might not have all the blockchain
        // so the target height is PeerHighestHight + target.
        // todo maybe
        //private int Security = 0;
        
        Dictionary<IPeer, int> PeerToHeights = new Dictionary<IPeer, int>();

        private Timer _cycleTimer;
        private IAElfNode _mainChainNode;

        public BlockSynchronizer(IAElfNode node, IPeerManager peerManager)
        {
            PendingBlocks = new List<PendingBlock>();
            _mainChainNode = node;
            _peerManager = peerManager;
        }

        public bool SetPeerHeight(IPeer peer, int height)
        {
            if (height < CurrentHeight)
                return false;
            
            if (PeerToHeights.ContainsKey(peer))
            {
                if (PeerToHeights[peer] >= height)
                    return false;
                
                PeerToHeights[peer] = height;
            }
            else
            {
                PeerToHeights.Add(peer, height);
            }

            return true;
        }

        internal async void DoCycle(object state)
        {
            if (IsInitialSync)
            {
                await ManagePeers();
            }
            
            //List<PendingBlock> blocksToExecute = PendingBlocks.Where(p => p.IsSynced).ToList();
            //await TryExecuteBlocks(blocksToExecute);
            
            // todo ask for txs
        }

        /// <summary>
        /// This method manages the peers used for downloading the blocks and also
        /// the what blocks are requested from who.
        /// </summary>
        /// <returns></returns>
        private async Task ManagePeers()
        {
            if (PeerToHeights == null)
                return;
                
            // It means we're still synchronizing old blocks 
                
            // In order to work we need at least one height from one of the peers
            if (PeerToHeights.Count > 0)
            {
                /** Calculate block requests to send **/
                    
                // todo use more that one peer
                    
                // for now we use only one - the one with the highest hight
                var peerHeightKvp = PeerToHeights.OrderByDescending(p => p.Value).First();

                if (peerHeightKvp.Value < CurrentHeight)
                    return; // As far as we know he's behind us

                IPeer peer = peerHeightKvp.Key;

                // Request the current block we're trying to sync
                BlockRequest br = new BlockRequest { Height = CurrentHeight };
                var req = NetRequestFactory.CreateRequest(MessageTypes.RequestBlock, br.ToByteArray(), null); 
                    
                await peer.SendAsync(req.ToByteArray());
            }
            else
            {
                // todo this is only executed if no peers have replied to the height request
                // todo if at least one peer as been set, the requests will not be broadcast
                    
                // Query peers for their height
                if (_peerManager != null)
                    await _peerManager.BroadcastMessage(MessageTypes.HeightRequest, new byte[] {}, 0);
            }
        }

        public void Start()
        {
            _cycleTimer = new Timer(DoCycle, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(60));
        }

        public void SetNodeHeight(int currentHeight)
        {
            // todo logic to handle a height change
            CurrentHeight = currentHeight;
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
                BlockExecutionResult res = await _mainChainNode.ExecuteAndAddBlock(block);
                
                if (res.Executed == false && res.ValidationError == ValidationError.OrphanBlock)
                {
                    // Here we've come across a block that is higher than the current
                    // chain height, we need to wait for it.
                    PendingBlock newPendingBlock = new PendingBlock(h, block, null);
                    newPendingBlock.IsWaitingForPrevious = true;
                    PendingBlocks.Add(newPendingBlock);
                }

                if (res.Executed == true && res.ValidationError == ValidationError.Success)
                {
                    CurrentHeight++;
                }
            }    
        }

        /// <summary>
        /// Tries to executes the specified blocks.
        /// </summary>
        /// <param name="pendingBlocks"></param>
        /// <returns></returns>
        internal async Task TryExecuteBlocks(List<PendingBlock> pendingBlocks)
        {
            List<PendingBlock> toRemove = new List<PendingBlock>();
            
            foreach (var pendingBlock in pendingBlocks)
            {
                Block block = pendingBlock.Block;
                BlockExecutionResult res = await _mainChainNode.ExecuteAndAddBlock(block);

                if (res.Executed)
                {
                    // The block was executed and validation was a success
                    if(res.ValidationError == ValidationError.Success)
                    {
                        // We can remove the pending block
                        toRemove.Add(pendingBlock);
                    }
                }
                else
                {
                    // The block wasn't executed
                    if (res.ValidationError == ValidationError.OrphanBlock)
                    {
                        // We ensure that the property is coherent
                        pendingBlock.IsWaitingForPrevious = true;
                    }
                    else
                    {
                        // todo deal with blocks that we're not executed
                    }
                }
            }
            
            // remove the pending blocks
            foreach (var pdBlock in PendingBlocks)
            {
                PendingBlocks.Remove(pdBlock);
            }
        }

        /// <summary>
        /// This adds a transaction to one off the blocks. Typically this happens when
        /// a transaction has been received throught the network (requested by this
        /// synchronizer).
        /// It removes the transaction from the corresponding missing block.
        /// </summary>
        /// <param name="txHash"></param>
        public bool SetTransaction(byte[] txHash)
        {
            PendingBlock b = RemoveTxFromBlock(txHash);
            return b != null;
        }

        public PendingBlock GetBlock(byte[] hash)
        {
            return PendingBlocks?.FirstOrDefault(p => p.BlockHash.BytesEqual(hash));
        }
        
        public PendingBlock RemoveTxFromBlock(byte[] hash)
        {
            foreach (var pdBlock in PendingBlocks)
            {
                foreach (var msTx in pdBlock.MissingTxs)
                {
                    if (msTx.BytesEqual(hash))
                    {
                        pdBlock.RemoveTransaction(msTx);
                        return pdBlock;
                    }
                }
            }

            return null;
        }
    }

    public class PendingBlock
    {
        public Block Block;
        
        public List<byte[]> MissingTxs { get; private set; }

        public bool IsWaitingForPrevious = false;
            
        public byte[] BlockHash { get; }

        public bool IsSynced
        {
            get { return MissingTxs.Count == 0; }
        }

        public PendingBlock(byte[] blockHash, Block block, List<Hash> missing)
        {
            Block = block;
            BlockHash = blockHash;
            
            MissingTxs = missing == null ? new List<byte[]>() : missing.Select(m => m.Value.ToByteArray()).ToList();
        }
        
        public void RemoveTransaction(byte[] txid)
        {
            MissingTxs.Remove(txid);
        }
    }
}