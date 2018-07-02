using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common.ByteArrayHelpers;
using AElf.Kernel.BlockValidationFilters;
using AElf.Kernel.Miner;
using AElf.Kernel.Node.Protocol.Exceptions;
using AElf.Kernel.Types;
using AElf.Network.Data;
using AElf.Network.Peers;
using Google.Protobuf;
using NLog;
using ServiceStack;

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

    public class Job
    {
        public bool IsSend { get; set; }
        public Block Block { get; set; }
        public Transaction Transaction { get; set; }
    }

    public class SyncPeer
    {
        public IPeer Peer { get; set; }
        public int? LastKnownHight { get; set; }
        public int RequestCount { get; set; } = 0;
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
        public event EventHandler SyncFinished;
        
        // React to new peers connected
        // when a peer connects get the height of his chain
        public IPeerManager _peerManager;
        private readonly ILogger _logger;

        private List<PendingBlock> PendingBlocks { get; }

        public bool IsInitialSync { get; set; } = true;

        // The height of the chain, this gets incremented each time a 
        // block is successfully removed.
        //public int CurrentHeight { get; private set; } = 0;
        public int CurrentExecHeight = 0;

        public int SyncTargetHeight = 0;
        
        // The peer with the highest hight might not have all the blockchain
        // so the target height is PeerHighestHight + target.
        // todo maybe
        //private int Security = 0;
        
        //Dictionary<IPeer, int> PeerToHeights = new Dictionary<IPeer, int>();
        private List<SyncPeer> _syncPeers = new List<SyncPeer>();

        private Timer _cycleTimer;
        private IAElfNode _mainChainNode;

        private BlockingCollection<Job> _jobQueue;

        private bool AskedForHeight = false;

        public BlockSynchronizer(IAElfNode node, IPeerManager peerManager)
        {
            PendingBlocks = new List<PendingBlock>();
            _jobQueue = new BlockingCollection<Job>();
            
            _mainChainNode = node;
            _peerManager = peerManager;
            _logger = LogManager.GetLogger("BlockSync");
            
            if (_peerManager.NoPeers)
            {
                FinishSync();
                _logger?.Trace("Finished sync : no peers.");
            }
            
            _peerManager.PeerListEmpty += OnPeerListEmpty;
        }
        
        public async Task Start()
        {
            if (IsInitialSync)
            {
                // Initialy connected nodes 
                List<IPeer> peers = _peerManager.GetPeers();
                
                peers.ForEach(p => _syncPeers.Add(new SyncPeer { Peer = p}));

                foreach (var syncPeer in _syncPeers)
                {
                    IPeer p = syncPeer.Peer;
                    
                    var req = NetRequestFactory.CreateRequest(MessageTypes.HeightRequest, new byte[] {}, 0);
                    await p.SendAsync(req.ToByteArray());

                    syncPeer.RequestCount++;
                    
                    Console.WriteLine(DateTime.Now.TimeOfDay + " [BlockSynchronizer] Broadcasting height request to " + p);
                }
            }
            
            _cycleTimer = new Timer(DoCycle, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(3));
            
            DoSync();
        }
        
        private void FinishSync()
        {
            IsInitialSync = false;
            //SyncFinished?.Invoke(this, EventArgs.Empty);
        }
        
        private void OnPeerListEmpty(object sender, EventArgs eventArgs)
        {
            if (_peerManager.NoPeers && IsInitialSync)
            {
                FinishSync();
                _logger?.Trace("Finished sync : no peers.");
            }
        }
        
        public bool SetPeerHeight(IPeer peer, int height)
        {
            if (!IsInitialSync)
                return false;

            if (height <= CurrentExecHeight)
            {
                if (_syncPeers.Count == 1)
                    FinishSync();
                    
                return false;
            }

            SyncPeer p = _syncPeers.FirstOrDefault(s => s.Peer.Equals(peer));

            if (p == null)
                return false;

            p.LastKnownHight = height;
            
            _logger?.Trace("Set peer height :: Peer " + peer + ", heigh " + height);

            if (SyncTargetHeight < height)
            {
                SyncTargetHeight = height;
                _logger?.Trace("New sync target height: " + height);
            }
            
            /*if (PeerToHeights.ContainsKey(peer))
            {
                if (PeerToHeights[peer] >= height)
                    return false;
                
                PeerToHeights[peer] = height;
            }
            else
            {
                PeerToHeights.Add(peer, height);
            }*/
            
            return true;
        }
        
        public List<IPeer> RemoveLowerHeightPeers()
        {
            /*if (PeerToHeights == null)
                return null;
            
            var toRemove = PeerToHeights.Where(kvp => kvp.Value <= CurrentExecHeight).ToList();
            
            foreach (var kvp in toRemove)
            {
                PeerToHeights.Remove(kvp.Key);
                _logger?.Trace("Clearing peer : " + kvp.Key);
            }

            return toRemove.Select(kvp => kvp.Key).ToList();*/
            return null;
        }

        internal async void DoCycle(object state)
        {
            if (IsInitialSync)
            {
                //RemoveLowerHeightPeers();
                await ManagePeers();
            }
            
            //List<PendingBlock> blocksToExecute = PendingBlocks.Where(p => p.IsSynced).ToList();
            //await TryExecuteBlocks(blocksToExecute);
            
            // todo ask for txs

            //await TryExecuteBlocks(PendingBlocks);
        }

        /// <summary>
        /// This method manages the peers used for downloading the blocks and also
        /// the what blocks are requested from who.
        /// </summary>
        /// <returns></returns>
        private async Task ManagePeers()
        {
            if (_syncPeers == null)
                return;
            
            // It means we're still synchronizing old blocks 
                
            // In order to work we need at least one height from one of the peers
            if (_syncPeers.Count > 0)
            {
                /** Calculate block requests to send **/
                    
                // todo use more that one peer
                    
                // for now we use only one - the one with the highest hight
                var peerHeightKvp = _syncPeers.Where(p => p.LastKnownHight.HasValue)
                    .OrderByDescending(p => p.LastKnownHight)
                    .FirstOrDefault();

                if (peerHeightKvp == null)
                {
                    _logger?.Trace("No peers have a know height.");
                    return;
                }

                if (peerHeightKvp.LastKnownHight <= CurrentExecHeight)
                    return; // As far as we know he's behind us

                IPeer peer = peerHeightKvp.Peer;

                // Request the current block we're trying to sync
                BlockRequest br = new BlockRequest { Height = CurrentExecHeight };
                var req = NetRequestFactory.CreateRequest(MessageTypes.RequestBlock, br.ToByteArray(), null); 
                    
                await peer.SendAsync(req.ToByteArray());
                
                _logger?.Trace("Block request for height " + CurrentExecHeight + " to " + peer);
            }
            else
            {
                // todo this is only executed if no peers have replied to the height request
                // todo if at least one peer as been set, the requests will not be broadcast
                
                // If InitSync is finished we don't query nodes for their height anymore
                /*if (IsInitialSync && AskedForHeight)
                {
                    // Query peers for their height
                    if (_peerManager != null)
                        await _peerManager.BroadcastMessage(MessageTypes.HeightRequest, new byte[] {}, 0);
                }*/
            }
        }

        public void SetNodeHeight(int currentHeight)
        {
            // todo logic to handle a height change
            CurrentExecHeight = currentHeight;
            
            _logger?.Trace("Current node height is set at " + CurrentExecHeight);
        }

        public void EnqueueJob(Job job)
        {
            try
            {
                Task.Run(() =>
                {
                    _jobQueue.Add(job);
                });
            }
            catch (Exception e)
            {
                _logger?.Trace("Error while adding " + job.Block.GetHash().Value.ToByteArray().ToHex());
            }
        }

        //AutoResetEvent theEvent = new AutoResetEvent(false);
        
        private void DoSync()
        {
            while (true)
            {
                Job j = null;

                try
                {
                    j = _jobQueue.Take();
                }
                catch (Exception e)
                {
                    _logger?.Trace("Error while dequeuing " + j?.Block.GetHash().Value.ToByteArray().ToHex());
                    continue;
                }

                try
                {
                    if (j.Transaction != null)
                    {
                        SetTransaction(j.Transaction.GetHash().Value.ToByteArray());
                    }
                    else
                    {
                        // Process transaction
                        
                        _logger?.Trace("Dequed block : " + j.Block.GetHash().Value.ToByteArray().ToHex());
                        
                        bool b = AddBlockToSync(j.Block).Result;
               
                        /* print candidates */

                        if (!b)
                        {
                            _logger.Trace("Could not add block to sync");
                        }
                    }

                    if (PendingBlocks == null || PendingBlocks.Count <= 0)
                    {
                        _logger.Trace("No pending blocks");
                        continue;
                    }
                    
                    var str = PendingBlocks.Select(bb => bb.ToString()).Aggregate((i, jf) => i + " || " + jf);
                    _logger?.Trace("Candidates for execution: " + str);
                
                    List<PendingBlock> bte = GetBlocksToExecute();
                    
                    if (bte == null || bte.Count <= 0)
                    {
                        _logger.Trace("No blocks to execute !");
                    }
                    else
                    {
                        var str2 = bte.Select(bb => bb.ToString()).Aggregate((i, jf) => i + " || " + jf);
                        _logger?.Trace("Chosen for execution: " + str2);
                
                        if (string.IsNullOrEmpty(str2))
                            _logger?.Trace("Nobody chosen for execution.");

                        if (bte != null && bte.Count > 0)
                        {
                            // Execute and log
                            List<PendingBlock> br = TryExecuteBlocks(bte).Result;

                            if (br != null && br.Count > 0)
                            {
                                StringBuilder brString = new StringBuilder();
                                brString.Append(br.ElementAt(0).Block.Header.Index);
                        
                                for (int i = 1; i < br.Count; i++)
                                {
                                    brString.Append(" - " + br.ElementAt(i).Block.Header.Index);
                                }
                        
                                _logger?.Trace("Executed the blocks with the following index(es) : " + brString);
                            }
                        }
                    }
                    
                    bool success = RequestMissingTxs().Result;

                }
                catch (Exception e)
                {
                    _logger?.Trace("Error while dequeuing and processing job.");
                }
            }
        }
        
        private async Task<bool> RequestMissingTxs()
        {
            if (_syncPeers == null || _syncPeers.Count <= 0)
                return false;

            var peerHeightKvp = _syncPeers.FirstOrDefault();
            
            List<byte[]> listOfMissingTxToRequest = new List<byte[]>();
            
            foreach (var pdBlock in PendingBlocks)
            {
                if (!pdBlock.IsSynced)
                {
                    foreach (var tx in pdBlock.MissingTxs)
                    {
                        if (listOfMissingTxToRequest.Count >= 5) // only 5 at a time
                            break;
                        
                        listOfMissingTxToRequest.Add(tx);
                    }
                }
            }

            IPeer peer = peerHeightKvp.Peer;
            
            foreach (var tx in listOfMissingTxToRequest)
            {
                TxRequest br = new TxRequest { TxHash = ByteString.CopyFrom(tx) };
                var req = NetRequestFactory.CreateRequest(MessageTypes.TxRequest, br.ToByteArray(), null);
                
                await peer.SendAsync(req.ToByteArray());
                
                _logger?.Trace("Request tx:" + br.TxHash.ToByteArray().ToHex());
            }

            return true;
        }

        internal List<PendingBlock> GetBlocksToExecute()
        {
            // Calculate the next batch to execute
            List<PendingBlock> ordered = PendingBlocks.Where(p => p.IsSynced).OrderBy(p => p.Block.Header.Index).ToList();
            
            if (ordered.Count <= 0)
                return new List<PendingBlock>();

            List<PendingBlock> pending = new List<PendingBlock>();

            int currentIndex = (int)ordered[0].Block.Header.Index;

            if (IsInitialSync && currentIndex > CurrentExecHeight)
                return null;
            
            for (int i = 0; i < ordered.Count; i++)
            {
                pending.Add(ordered[i]);

                if (i+1 >= ordered.Count || (int) ordered[i+1].Block.Header.Index > currentIndex + 1)
                    break;
                
                currentIndex = (int)ordered[i+1].Block.Header.Index;
            }
            
            return pending;
        }
        
        /// <summary>
        /// When a block is received through the network it is placed here for sync
        /// purposes. In the case that the transaction was not received through the
        /// network, it will be placed here to sync.
        /// </summary>
        /// <param name="block"></param>
        private async Task<bool> AddBlockToSync(Block block)
        {
            if (block?.Header == null || block.Body == null)
                throw new InvalidBlockException("The block, blockheader or body is null");
            
            /*if (block.Body.Transactions == null || block.Body.Transactions.Count <= 0)
                throw new InvalidBlockException("The block contains no transactions");*/
            
            if (block.Header.Index < (ulong)CurrentExecHeight)
                return false;

            byte[] h = null;
            try
            {
                h = block.GetHash().GetHashBytes();
            }
            catch (Exception e)
            {
                //theEvent.WaitOne();
                throw new InvalidBlockException("Invalid block hash");
            }
            
            if (GetBlock(h) != null)
            {
                _logger?.Trace("Block already in pending list.");
                return false;
            }

            List<Hash> missingTxs = _mainChainNode.GetMissingTransactions(block);

            if (missingTxs == null)
            {
                //theEvent.WaitOne();
                // todo what happend when the pool fails ?
                return false;
            }
            
            // todo check that the returned txs are actually in the block
            PendingBlock newPendingBlock = new PendingBlock(h, block, missingTxs);
            PendingBlocks.Add(newPendingBlock);
            
            _logger?.Trace("Added block to sync : " + h.ToHex());
            
            return true;
        }
        
        private object objLock = new object();

        /// <summary>
        /// Tries to executes the specified blocks.
        /// </summary>
        /// <param name="pendingBlocks"></param>
        /// <returns></returns>
        internal async Task<List<PendingBlock>> TryExecuteBlocks(List<PendingBlock> pendingBlocks)
        {
            List<PendingBlock> toRemove = new List<PendingBlock>();
            List<PendingBlock> executed = new List<PendingBlock>();

            var blcks = pendingBlocks.ToList();
            foreach (var pendingBlock in blcks)
            {
                Block block = pendingBlock.Block;
                
                if (_mainChainNode.IsMiningInProcess == 1)
                    _logger?.Trace("----- MINING !!");
                    
                BlockExecutionResult res = await _mainChainNode.ExecuteAndAddBlock(block);
                
                _logger?.Trace($"Block execution result : {res.Executed}, {res.ValidationError} : { block.GetHash().Value.ToByteArray().ToHex() } - Index {block.Header.Index}");

                if (res.Executed)
                {
                    // The block was executed and validation was a success
                    if(res.ValidationError == ValidationError.Success)
                    {
                        // We can remove the pending block
                        toRemove.Add(pendingBlock);
                        executed.Add(pendingBlock);
                        CurrentExecHeight++;
                    }
                }
                else
                {
                    // The block wasn't executed
                    if (res.ValidationError == ValidationError.Pending)
                    {
                        // We ensure that the property is coherent
                        pendingBlock.IsWaitingForPrevious = true;
                        //_logger?.Trace("-- Pending block at height " + pendingBlock.Block.Header.Index);
                    }
                    else if (res.ValidationError == ValidationError.AlreadyExecuted)
                    {
                        toRemove.Add(pendingBlock);
                        //_logger?.Trace("Block { " + Convert.ToBase64String(pendingBlock.BlockHash) + " } at height " + pendingBlock.Block.Header.Index + " was already executed.");
                    }
                    else
                    {
                        toRemove.Add(pendingBlock);
                        //_logger?.Trace("-- Other situationat height " + pendingBlock.Block.Header.Index);
                        // todo deal with blocks that we're not executed
                    }
                }
            }
            
            // remove the pending blocks
            foreach (var pdBlock in toRemove)
            {
                lock (objLock)
                {
                    PendingBlocks.Remove(pdBlock);
                }
            }
            
            if (IsInitialSync && CurrentExecHeight >= SyncTargetHeight)
            {
                IsInitialSync = false;
                _logger?.Trace("-- Initial sync is finished at height: " + CurrentExecHeight);
                            
                SyncFinished?.Invoke(this, EventArgs.Empty);
            }

            return executed;
        }

        /// <summary>
        /// This adds a transaction to one off the blocks. Typically this happens when
        /// a transaction has been received throught the network (requested by this
        /// synchronizer).
        /// It removes the transaction from the corresponding missing block.
        /// </summary>
        /// <param name="txHash"></param>
        private bool SetTransaction(byte[] txHash)
        {
            PendingBlock b = RemoveTxFromBlock(txHash);

            if (b == null)
                return false;
            
            _logger?.Trace("Transaction removed from sync: " + txHash.ToHex());
            
            return true;
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

        public override string ToString()
        {
            return "{ " + BlockHash.ToHex() + ", " + IsSynced + ", " + Block?.Header?.Index + " }";
        }
    }
}