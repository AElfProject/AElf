using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common.ByteArrayHelpers;
using AElf.Common.Collections;
using AElf.Kernel.Node.Protocol.Exceptions;
using AElf.Network.Data;
using AElf.Network.Peers;
using Google.Protobuf;
using NLog;
using AElf.ChainController;
using AElf.Network;
using AElf.Network.Connection;
using AElf.Network.Data.Protobuf;

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
        public bool IsWakeUp { get; set; }
        public Block Block { get; set; }
        public Transaction Transaction { get; set; }
    }

//    public class SyncPeer
//    {
//        public const int AlreadyRequestedQueueLimit = 5;
//        
//        public IPeer Peer { get; set; }
//        public int? LastKnownHight { get; set; }
//        public int RequestCount { get; set; } = 0;
//        
//        public BoundedByteArrayQueue AlreadyRequested { get; set; }
//
//        public SyncPeer()
//        {
//            AlreadyRequested = new BoundedByteArrayQueue(AlreadyRequestedQueueLimit);
//        }
//    }
    
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
        public event EventHandler SyncFinished;

        public INetworkManager _networkManager;
        private readonly ILogger _logger;

        public List<PendingBlock> PendingBlocks { get; }

        public bool ShouldDoInitialSync { get; private set; } = false;
        public bool IsInitialSyncInProgress { get; private set; } = false;

        public int CurrentExecHeight = 1;

        public int SyncTargetHeight = 0;

        //private Timer _cycleTimer;
        private IAElfNode _mainChainNode;

        private BlockingCollection<Job> _jobQueue;

        public BlockSynchronizer(INetworkManager networkManager, IAElfNode node)
        {
            PendingBlocks = new List<PendingBlock>();
            _jobQueue = new BlockingCollection<Job>();
            
            _mainChainNode = node;
            _networkManager = networkManager;
            
            _logger = LogManager.GetLogger("BlockSync");
            
            _networkManager.MessageReceived += ProcessPeerMessage;
        }
        
        public async Task Start(bool doInitialSync)
        {
            ShouldDoInitialSync = doInitialSync;
            IsInitialSyncInProgress = false;
            
            if (doInitialSync)
                _logger?.Trace("Initial sync started.");
        }

        private void ProcessPeerMessage(object sender, EventArgs e)
        {
            if (sender != null && e is NetMessageReceived args && args.Message != null)
            {
                Message message = args.Message;
                MessageType msgType = (MessageType)message.Type;

                if (msgType == MessageType.Tx)
                {
                    try
                    {
                        var fromSend = message.Type == (int) MessageType.Tx;
                
                        _mainChainNode.ReceiveTransaction(message.Payload, fromSend);
                    }
                    catch (Exception ex)
                    {
                        _logger?.Trace(ex, "Error while receiving transaction.");
                        return;
                    }
                    
                    EnqueueJob(new Job { Transaction = Transaction.Parser.ParseFrom(message.Payload) });
                }
                else if (message.Type == (int)MessageType.Block || message.Type == (int)MessageType.BroadcastBlock)
                {
                    try
                    {
                        Block b = Block.Parser.ParseFrom(message.Payload);
                        
                        EnqueueJob(new Job { Block = b });
                        _logger?.Trace("Block enqueued: " + b.GetHash().ToHex());
                    }
                    catch (Exception ex)
                    {
                        _logger?.Trace(ex, "Error while receiving HandleBlockReception.");
                    }
                }
            }
        }

        private void FinishSync()
        {
            ShouldDoInitialSync = false;
            //SyncFinished?.Invoke(this, EventArgs.Empty);
        }

//        internal async void DoCycle(object state)
//        {
//            if (ShouldDoInitialSync)
//            {
//                //RemoveLowerHeightPeers();
//                await ManagePeers();
//            }
//            else
//            {
//                if (_jobQueue.Count <= 0)
//                    EnqueueJob(new Job { IsWakeUp = true });
//            }
//        }

        // TODO move this logic into the network manager
        
        /// <summary>
        /// This method manages the peers used for downloading the blocks and also
        /// the what blocks are requested from who.
        /// </summary>
        /// <returns></returns>
//        private async Task ManagePeers()
//        {
//            if (_syncPeers == null)
//                return;
//            
//            // It means we're still synchronizing old blocks 
//                
//            // In order to work we need at least one height from one of the peers
//            if (_syncPeers.Count > 0)
//            {
//                // for now we use only one - the one with the highest hight
//                var peerHeightKvp = _syncPeers.Where(p => p.LastKnownHight.HasValue)
//                    .OrderByDescending(p => p.LastKnownHight)
//                    .FirstOrDefault();
//
//                if (peerHeightKvp == null)
//                {
//                    _logger?.Trace("No peers have a know height.");
//                    return;
//                }
//
//                if (peerHeightKvp.LastKnownHight <= CurrentExecHeight)
//                    return; // As far as we know he's behind us
//                
//                IPeer peer = peerHeightKvp.Peer;
//                
//                await SendBlockRequest(peer, CurrentExecHeight);
//            }
//        }
//
//        private async Task SendBlockRequest(IPeer peer, int height)
//        {
//            if (_syncPeers.Count > 0)
//            {
//                // Request the current block we're trying to sync
//                BlockRequest br = new BlockRequest { Height = CurrentExecHeight };
//                var req = NetRequestFactory.CreateMessage(MessageType.RequestBlock, br.ToByteArray(), null); 
//                    
//                peer.EnqueueOutgoing(req);
//                
//                _logger?.Trace("Block request for height " + CurrentExecHeight + " to " + peer);
//            }
//        }

//        public void SetNodeHeight(int currentHeight)
//        {
//            // todo logic to handle a height change
//            CurrentExecHeight = currentHeight;
//            
//            _logger?.Trace("Current node height is set at " + CurrentExecHeight);
//        }

        public void EnqueueJob(Job job)
        {
            try
            {
                if (job?.Block?.Header != null && ShouldDoInitialSync && !IsInitialSyncInProgress)
                {
                    // The first block we receive when a node should synchronize will determine the 
                    // target height
                    SyncTargetHeight = (int) job.Block.Header.Index;
                    IsInitialSyncInProgress = true;

                    for (int i = 1; i < SyncTargetHeight; i++)
                    {
                        _networkManager.QueueBlockRequestByIndex(i);
                    }
                    
                    Task.Run(() => DoSync());
                    
                    _logger?.Trace($"Initial synchronisation has started at target height {SyncTargetHeight}.");
                }
                
                Task.Run(() => { _jobQueue.Add(job); });
            }
            catch (Exception e)
            {
                _logger?.Trace("Error while adding " + job.Block.GetHash().Value.ToByteArray().ToHex());
            }
        }
        
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
                    _logger?.Trace("Error while dequeuing " + j?.Block.GetHash().ToHex());
                    continue;
                }

                try
                {
                    if (!j.IsWakeUp)
                    {
                        if (j.Transaction != null)
                        {
                            // Process transaction
                            SetTransaction(j.Transaction.GetHash().GetHashBytes());
                        }
                        else
                        {
                            // Process block
                            _logger?.Trace("Dequed block : " + j.Block.GetHash().ToHex());

                            var b = AddBlockToSync(j.Block).Result;

                            /* print candidates */

                            if (!b)
                            {
                                _logger.Trace("Could not add block to sync");
                            }
                        }
                    }
                    
                    if (PendingBlocks == null || PendingBlocks.Count <= 0)
                    {
                        _logger.Trace("No pending blocks");
                        continue;
                    }

                    var str = PendingBlocks.Take(10).OrderBy(bb => bb.Block.Header.Index).Select(bb => bb.ToString()).Aggregate((i, jf) => i + " || " + jf);
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
                    
                    // Get missing txs 
                    RequestMissingTxs();
                }
                catch (Exception e)
                {
                    _logger?.Trace(e, "Error while dequeuing and processing job.");
                }
            }
        }
        
        private void RequestMissingTxs()
        {
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

            foreach (var tx in listOfMissingTxToRequest)
            {
                _networkManager.QueueTransactionRequest(tx);
            }
        }

        internal List<PendingBlock> GetBlocksToExecute()
        {
            // Calculate the next batch to execute
            List<PendingBlock> ordered = PendingBlocks.Where(p => p.IsSynced).OrderBy(p => p.Block.Header.Index).ToList();
            
            if (ordered.Count <= 0)
                return new List<PendingBlock>();

            List<PendingBlock> pending = new List<PendingBlock>();

            int currentIndex = (int)ordered[0].Block.Header.Index;

            if (ShouldDoInitialSync && currentIndex > CurrentExecHeight)
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
            
//            if (block.Header.Index < (ulong)CurrentExecHeight)
//                return false;

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
                {
                    _logger?.Trace("----- MINING !!");
                    return toRemove;
                }

                BlockExecutionResult res = await _mainChainNode.ExecuteAndAddBlock(block);
                _logger?.Trace(
                    $"TryExecuteBlocks - Block execution result : {res.Executed}, {res.ValidationError} : {block.GetHash().Value.ToByteArray().ToHex()} - Index {block.Header.Index}");
                if (res.ValidationError == ValidationError.Success && res.Executed)
                {
                    // The block was executed and validation was a success: remove the pending block.
                    toRemove.Add(pendingBlock);
                    executed.Add(pendingBlock);
                    Interlocked.Increment(ref CurrentExecHeight);
                }
                else
                {
                    // The block wasn't executed or validation failed
                    if (res.ValidationError == ValidationError.AlreadyExecuted || res.ValidationError == ValidationError.OrphanBlock)
                    {
                        // The block is an earlier block and one with the same
                        // height as already been executed so it can safely be
                        // remove from the pending blocks.
                        toRemove.Add(pendingBlock);
                    }
                    else if (res.ValidationError == ValidationError.Pending)
                    {
                        // The current blocks index is higher than the current height so we're missing
                        if (!ShouldDoInitialSync && (int) block.Header.Index > CurrentExecHeight)
                        {
//                            if (_syncPeers.Count > 0)
//                            {
//                                // for now we use only one - the one with the highest hight
//                                var peerHeightKvp = _syncPeers
//                                    .Where(p => !p.AlreadyRequested.Contains(pendingBlock.BlockHash))
//                                    .OrderByDescending(p => p.LastKnownHight)
//                                    .FirstOrDefault();
//                                if (peerHeightKvp != null)
//                                {
//                                    _logger?.Trace("Missing block, request for height : " + CurrentExecHeight +
//                                                   ", to : " + peerHeightKvp.Peer);
//                                    
//                                    await SendBlockRequest(peerHeightKvp.Peer, CurrentExecHeight);
//                                    
//                                    peerHeightKvp.AlreadyRequested.Enqueue(pendingBlock.BlockHash);
//                                }
//                                else
//                                {
//                                    _logger?.Trace("All peers already tried for request.");
//                                }
//                            }
                            
                             _networkManager.QueueBlockRequestByIndex(CurrentExecHeight);

                            // At this point no need to execute more
                            break;
                        }
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

            if (ShouldDoInitialSync && CurrentExecHeight >= SyncTargetHeight)
            {
                ShouldDoInitialSync = false;
                IsInitialSyncInProgress = false;
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
            var b = RemoveTxFromBlock(txHash);

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