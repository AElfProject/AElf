using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.ChainController.EventMessages;
using AElf.Common;
using AElf.Common.FSM;
using AElf.Kernel;
using AElf.Kernel.EventMessages;
using AElf.Kernel.Managers;
using AElf.Node.EventMessages;
using AElf.Synchronization.BlockExecution;
using AElf.Synchronization.EventMessages;
using Easy.MessageHub;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Synchronization.BlockSynchronization
{
    // ReSharper disable InconsistentNaming
    public class BlockSynchronizer : IBlockSynchronizer
    {        
        public ILogger<BlockSynchronizer> Logger {get;set;}
        
        private readonly IChainService _chainService;
        private readonly IBlockValidationService _blockValidationService;
        private readonly IMinersManager _minersManager;
        private readonly IBlockExecutor _blockExecutor;
        
        private readonly FSM<NodeState> _stateFsm;
        private readonly BlockSet _blockSet;
        private readonly object _blockCacheLock = new object();
        private readonly List<IBlock> _blockCache = new List<IBlock>();
        
        private IBlockChain _blockChain;

        private NodeState CurrentState => _stateFsm.CurrentState;
        
        private IBlock _currentBlock;

        public int RollBackTimes { get; private set; }

        public BlockState HeadBlock { get; private set; }
        public BlockState CurrentLib { get; private set; }

        private List<string> _currentMiners;

        private int _chainId;
        
        private bool IsSwitching;

        public BlockSynchronizer(IChainService chainService, IBlockValidationService blockValidationService,
            IBlockExecutor blockExecutor, IMinersManager minersManager)
        {
            _chainService = chainService;
            _blockValidationService = blockValidationService;
            _blockExecutor = blockExecutor;
            _minersManager = minersManager;

            _blockSet = new BlockSet();
            _stateFsm = new NodeStateFSM().Create();

            Logger= NullLogger<BlockSynchronizer>.Instance;

            MessageHub.Instance.Subscribe<StateEvent>(e =>
            {
                _stateFsm.ProcessWithStateEvent(e);
                MessageHub.Instance.Publish(new FSMStateChanged(CurrentState));
            });

            MessageHub.Instance.Subscribe<EnteringState>(async inState =>
            {
                if (inState.NodeState.ShouldLockMiningWhenEntering())
                {
                    MessageHub.Instance.Publish(new LockMining(true));
                }
                
                switch (inState.NodeState)
                {
                    case NodeState.Catching:
                    case NodeState.Caught:
                        _currentBlock = null;
                        await TryExecuteNextCachedBlock();
                        break;

                    case NodeState.ExecutingLoop:
                    case NodeState.GeneratingConsensusTx:
                        MessageHub.Instance.Publish(new LockMining(false));
                        break;
                }
            });

            MessageHub.Instance.Subscribe<LeavingState>(inState =>
            {
                if (inState.NodeState.ShouldUnlockMiningWhenLeaving())
                    MessageHub.Instance.Publish(new LockMining(false));
            });

            MessageHub.Instance.Subscribe<HeadersReceived>(async inHeaders =>
            {
                if (inHeaders?.Headers == null || !inHeaders.Headers.Any())
                {
                    Logger.LogWarning("Null headers or header list is empty.");
                    return;
                }

                var headers = inHeaders.Headers.OrderByDescending(h => h.Height).ToList();

                foreach (var blockHeader in headers)
                {
                    // Get previous block from the chain
                    var correspondingBlockHeader = await _blockChain.GetBlockByHeightAsync(blockHeader.Height - 1);

                    // If the hash of this previous block corresponds to "previous block hash" of the current header
                    // the link has been found
                    if (correspondingBlockHeader.BlockHashToHex == blockHeader.PreviousBlockHash.ToHex())
                    {
                        // Launch header accepted event and return
                        MessageHub.Instance.Publish(new HeaderAccepted(blockHeader));
                        return;
                    }
                }

                // Launch unlinkable again with the last headers index 
                MessageHub.Instance.Publish(new UnlinkableHeader(headers.Last()));
            });

            MessageHub.Instance.Subscribe<BlockMined>(inBlock =>
            {
                AddMinedBlock(inBlock.Block);
            });
            
            MessageHub.Instance.Subscribe<BlockReceived>(async inBlock =>
            {
                if (inBlock.Block == null)
                    return;
                    
                Logger.LogDebug($"Handling {inBlock.Block} - state {CurrentState}");
            
                CacheBlock(inBlock.Block);
            
                await TryExecuteNextCachedBlock();
            });
            
            MessageHub.Instance.Subscribe<MinorityForkDetected>(async inBlock => { await OnMinorityForkDetected(); });
        }

        private async Task OnMinorityForkDetected()
        {
            IsSwitching = true;
            // Get ourselves into the Reverting state 
            MessageHub.Instance.Publish(StateEvent.LongerChainDetected);
            
            Logger.LogWarning("The chain is about to be re-synced from the lib.");

            try
            {
                lock (_blockCacheLock)
                {
                    // clear cache queue - this is to at least forget about current blocks in the queue
                    // if some get added right after, they won't be valid (unlinkable)
                    _blockCache.Clear();
                    _blockSet.Clear();
                }
            
                // We rollback to LIB if it exists
                if (CurrentLib != null && CurrentLib.Index != 0)
                {
                    await _blockChain.RollbackToHeight(CurrentLib.Index);
                    HeadBlock = CurrentLib;
                }
                else
                {
                    Logger.LogWarning("No LIB found...");
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error while handling minority fork situation.");
            }
            
            MessageHub.Instance.Publish(StateEvent.RollbackFinished);
            IsSwitching = false;

            var t = TryExecuteNextCachedBlock();
        }

        public void Init(int chainId)
        {
            try
            {
                _chainId = chainId;
                _blockChain = _chainService.GetBlockChain(_chainId);
            
                Miners miners = _minersManager.GetMiners(0).Result;
            
                _currentMiners = new List<string>();

                foreach (var miner in miners.PublicKeys)
                {
                    _currentMiners.Add(miner);
                    Logger.LogDebug($"Added a miner {miner}");
                }
            
                var height = _blockChain.GetCurrentBlockHeightAsync().Result;
                var currentBlock = _blockChain.GetBlockByHeightAsync(height).Result as Block;
            
                HeadBlock = _blockSet.Init(_currentMiners, currentBlock);
                _blockSet.LibChanged += BlockSetOnLibChanged;

                if (HeadBlock.Index == GlobalConfig.GenesisBlockHeight)
                    CurrentLib = HeadBlock;
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error while initializing block sync.");
            }
        }

        private void BlockSetOnLibChanged(object sender, EventArgs e)
        {
            if (e is LibChangedArgs libChangedArgs)
            {
                CurrentLib = libChangedArgs.NewLib;
                Logger.LogDebug($"New lib found: {{ id: {CurrentLib.BlockHash}, height: {CurrentLib.Index} }}");
                
                MessageHub.Instance.Publish(new NewLibFound
                {
                    Height = libChangedArgs.NewLib.Index,
                    BlockHash = libChangedArgs.NewLib.BlockHash
                });
            }
        }

        /// <summary>
        /// If the block CurrentHeight + 1 on current fork exists, then execute it.
        /// </summary>
        private async Task TryExecuteNextCachedBlock()
        {
            // no handling of blocks in other states than Catching/Caught (case where the node is busy).
            if (_stateFsm.CurrentState != NodeState.Catching && _stateFsm.CurrentState != NodeState.Caught)
            {
                IncorrectStateLog(nameof(TryExecuteNextCachedBlock));
                return;
            }

            if (_currentBlock != null)
            {
                Logger.LogDebug("Current not null, returning");
                return;
            }

            if (IsSwitching)
            {
                Logger.LogDebug("Switching...");
                return;
            }

            IBlock next;
            lock (_blockCacheLock)
            {
                if (!_blockCache.Any())
                    return;
                
                if (_blockCache.Count > 1)
                    Logger.LogDebug($"Info log cache count is high {_blockCache.Count}.");

                // execute the block with the lowest index
                next = _blockCache.OrderBy(b => b.Height).FirstOrDefault();

                if (next == null)
                    return;

                if (next.Height > HeadBlock.Index + 1)
                {
                    Logger.LogWarning($"Future block {next}, current height {HeadBlock.Index}, don't handle it.");
                    return;
                }

                _blockCache.Remove(next);

                _currentBlock = next;
                
                Logger.LogDebug($"Removed from cache : {next}");
            }
            
            await TryPushBlock(next);
        }

        /// <summary>
        /// Puts the block in the cache and ensures uniqueness.
        /// </summary>
        /// <returns></returns>
        private void CacheBlock(IBlock block)
        {
            lock (_blockCacheLock)
            {
                if (_blockCache.Any(b => b.GetHash() == block.GetHash()))
                    return;
                    
                _blockCache.Add(block);
            }
        }

        /// <summary>
        /// Called when receiving a block through the network -or- NodeState.Catching/NodeState.Caught state
        /// change has triggered the <see cref="TryExecuteNextCachedBlock"/> method.
        /// </summary>
        public async Task TryPushBlock(IBlock block)
        {
            
            if (block == null)
                throw new ArgumentNullException(nameof(block), "The block cannot be null");
            
            try
            {
                // Catching/Caught -> BlockValidating
                MessageHub.Instance.Publish(StateEvent.ValidBlockHeader);
                
                // todo check existence in database
            
                if (block.Height > HeadBlock.Index + 1)
                {
                    Logger.LogWarning($"Future block {block}, current height {HeadBlock.Index} ");
                    MessageHub.Instance.Publish(StateEvent.InvalidBlock); // get back to Catching
                    return;
                }
                
                if (_blockSet.IsBlockReceived(block))
                {
                    Logger.LogWarning($"Block already known {block}, current height {HeadBlock.Index} ");
                    MessageHub.Instance.Publish(StateEvent.InvalidBlock); // get back to Catching
                    return;
                }
                
                try
                {
                    _blockSet.PushBlock(block);
                }
                catch (UnlinkableBlockException)
                {
                    Logger.LogWarning($"Block unlinkable {block}");
                    MessageHub.Instance.Publish(StateEvent.InvalidBlock); // get back to Catching
                    // todo event on unlinkable
                    MessageHub.Instance.Publish(new BlockRejected(block));
                    return;
                }
            
                MessageHub.Instance.Publish(new BlockAccepted(block));

                Logger.LogTrace($"Pushed {block}, current state {CurrentState}, current head {HeadBlock}, blockset head {_blockSet.CurrentHead.BlockHash}");

                if (HeadBlock.BlockHash != _blockSet.CurrentHead.BlockHash)
                {
                    if (HeadBlock.BlockHash != _blockSet.CurrentHead.Previous)
                    {
                        // Here the blockset has switched fork -> attempt to switch the blockchain
                        await TrySwitchFork();
                    }
                    else
                    {
                        await HandleBlock(block);
                    }
                }
                else
                {
                    // not really invalid, just to go back to catching
                    MessageHub.Instance.Publish(StateEvent.InvalidBlock);
                    
                    Logger.LogDebug($"Block {block} has been linked.");
                    MessageHub.Instance.Publish(new BlockLinked(block));
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error while handling a block.");
            }
        }

        private async Task HandleBlock(IBlock block)
        {
            if (_stateFsm.CurrentState != NodeState.BlockValidating)
            {
                IncorrectStateLog(nameof(HandleBlock));
                return;
            }

            var blockValidationResult = await _blockValidationService.ValidateBlockAsync(block);
            Logger.LogInformation($"Block validation result {block} - {blockValidationResult}");

            if (blockValidationResult.IsSuccess())
            {
                // BlockValidating -> BlockExecuting
                MessageHub.Instance.Publish(StateEvent.ValidBlock);
                await HandleValidBlock(block);
            }
            else
            {
                Logger.LogWarning( $"Invalid block ({blockValidationResult}) {block}");
                MessageHub.Instance.Publish(StateEvent.InvalidBlock);
                MessageHub.Instance.Publish(new BlockRejected(block));
                MessageHub.Instance.Publish(new LockMining(false));
                _blockSet.RemoveInvalidBlock(block.GetHash());
            }
        }

        private async Task HandleValidBlock(IBlock block)
        {
            if (_stateFsm.CurrentState != NodeState.BlockExecuting)
            {
                IncorrectStateLog(nameof(HandleValidBlock));
                return;
            }

            var executionResult = await _blockExecutor.ExecuteBlock(block);

            Logger.LogTrace($"Execution result of block {block} - {executionResult}");
            
            if (executionResult == BlockExecutionResult.AlreadyAppended)
            {
                // todo blocks should not be already appended when rollbacking
            }
            else if (executionResult == BlockExecutionResult.BlockIsNull || 
                     executionResult == BlockExecutionResult.NoTransaction)
            {
                // todo Should be validated before => should throw exception
            }
            else if (executionResult == BlockExecutionResult.Mining 
                     || executionResult == BlockExecutionResult.Terminated)
            {
                // todo should not happen, related to locking => should throw exception
            }
            else if (executionResult == BlockExecutionResult.Fatal
                     || executionResult == BlockExecutionResult.ExecutionCancelled
                     || executionResult == BlockExecutionResult.IncorrectStateMerkleTree)
            {
                // The block can be considered bad.
                _blockSet.RemoveInvalidBlock(block.GetHash());
                
                MessageHub.Instance.Publish(StateEvent.StateUpdated); // todo just get back to catching
                MessageHub.Instance.Publish(StateEvent.BlockAppended); // todo just get back to catching
                
                MessageHub.Instance.Publish(new BlockRejected(block));
                return;
            }
            else if (executionResult.IsSideChainError())
            {
                // todo side chain logic
                // these cases are currently unhandled 
                _blockSet.RemoveInvalidBlock(block.GetHash());
                MessageHub.Instance.Publish(StateEvent.StateNotUpdated);
                return;
            }

            // if the current chain has just been extended and the block was successfully
            // executed we can change the synchronizers head.
            if (HeadBlock == _blockSet.CurrentHead.PreviousState)
                HeadBlock = _blockSet.CurrentHead;

            MessageHub.Instance.Publish(UpdateConsensus.UpdateAfterExecution);

            // BlockAppending -> Catching / Caught
            MessageHub.Instance.Publish(StateEvent.BlockAppended);
            
            MessageHub.Instance.Publish(new BlockExecuted(block));
        }
        
        /// <summary>
        /// Method called to switch the current branch to a longer one.
        /// </summary>
        private async Task TrySwitchFork()
        {
            try
            {
                // We should come from either Catching or Caught
                if (CurrentState != NodeState.BlockValidating)
                    Logger.LogWarning("Unexpected state...");

                if (HeadBlock == _blockSet.CurrentHead)
                {
                    Logger.LogWarning($"Current head already the same as block set: {HeadBlock}.");
                    MessageHub.Instance.Publish(StateEvent.InvalidBlock); // get back to Catching
                    return;
                }
            
                // Get ourselves into the Reverting state 
                MessageHub.Instance.Publish(StateEvent.LongerChainDetected);
            
                // Dispose consensus (reported from previous logic)
                MessageHub.Instance.Publish(UpdateConsensus.Dispose);

                // CurrentHead to BlockSet.CurrentHead
                List<BlockState> toexec = _blockSet.GetBranch(_blockSet.CurrentHead, HeadBlock);
                toexec = toexec.OrderBy(b => b.Index).ToList();
                
                Logger.LogDebug($"Attempting to switch fork: {HeadBlock} to {_blockSet.CurrentHead}.");
            
                await _blockChain.RollbackToHeight(toexec.First().Index);

                // exec blocks one by one (skip root of both forks because it's not rollbacked)
                foreach (var block in toexec.Skip(1))
                {
                    var executionResult = await _blockExecutor.ExecuteBlock(block.GetClonedBlock());
                    Logger.LogTrace($"Execution of {block} : {executionResult}");
                }

                HeadBlock = _blockSet.CurrentHead;

                RollBackTimes++;
                
                MessageHub.Instance.Publish(new BlockExecuted(toexec.Last().GetClonedBlock()));
                
                // Reverting -> Catching
                MessageHub.Instance.Publish(StateEvent.RollbackFinished);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error while trying to switch chain.");
            }
        }

        /// <summary>
        /// Callback for blocks mined by this node.
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        public void AddMinedBlock(IBlock block)
        {
            try
            {
                _blockSet.PushBlock(block);
                        
                // if the current chain has just been extended update
                if (HeadBlock == _blockSet.CurrentHead.PreviousState)
                {
                    HeadBlock = _blockSet.CurrentHead;
                    Logger.LogTrace($"Pushed {block}, current head {HeadBlock}, current state {CurrentState}");
                }
                else
                {
                    Logger.LogWarning($"Mined block did not extend current chain ! Pushed {block}, current head {HeadBlock}, current state {CurrentState}");
                }
            }
            catch (UnlinkableBlockException)
            {
                Logger.LogWarning($"Block unlinkable {block}");
            }
        }

        private async Task<IChainContext> GetChainContextAsync()
        {
            IChainContext chainContext = new ChainContext
            {
                ChainId = _chainId,
                BlockHash = await _blockChain.GetCurrentBlockHashAsync()
            };

            if (chainContext.BlockHash != Hash.Genesis && chainContext.BlockHash != null)
            {
                chainContext.BlockHeight =
                    ((BlockHeader) await _blockChain.GetHeaderByHashAsync(chainContext.BlockHash)).Height;
            }

            return chainContext;
        }

        public IBlock GetBlockByHash(Hash blockHash)
        {
            return _blockCache.FirstOrDefault(b => b.GetHash() == blockHash) ?? _blockSet.GetBlockByHash(blockHash) ?? _blockChain.GetBlockByHashAsync(blockHash).Result;
        }

        public async Task<BlockHeaderList> GetBlockHeaderList(ulong index, int count)
        {
            var blockHeaderList = new BlockHeaderList();
            for (var i = index; i > index - (ulong) count; i--)
            {
                var block = await _blockChain.GetBlockByHeightAsync(i);
                blockHeaderList.Headers.Add(block.Header);
            }

            return blockHeaderList;
        }

        private void IncorrectStateLog(string methodName)
        {
            Logger.LogTrace($"Incorrect fsm state: {_stateFsm.CurrentState.ToString()} in method {methodName}");
        }
    }
}