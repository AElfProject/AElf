using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.ChainController.EventMessages;
using AElf.Common;
using AElf.Common.Attributes;
using AElf.Common.FSM;
using AElf.Configuration;
using AElf.Configuration.Config.Chain;
using AElf.Kernel;
using AElf.Kernel.EventMessages;
using AElf.Kernel.Managers;
using AElf.Kernel.Types.Common;
using AElf.Miner.EventMessages;
using AElf.Node.EventMessages;
using AElf.Synchronization.BlockExecution;
using AElf.Synchronization.EventMessages;
using Easy.MessageHub;
using NLog;

namespace AElf.Synchronization.BlockSynchronization
{
    [LoggerName(nameof(BlockSynchronizer))]
    public class BlockSynchronizer : IBlockSynchronizer
    {
        private bool _terminated;
        
        private readonly ILogger _logger;
        
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
        
        private bool _executeNextBlock;
        private IBlock _currentBlock;

        public int RollBackTimes { get; private set; }

        public BlockState HeadBlock { get; private set; }
        public BlockState CurrentLib { get; private set; }

        private List<string> _currentMiners;

        private Hash _chainId;
        private string _nodePubKey;

        public BlockSynchronizer(IChainService chainService, IBlockValidationService blockValidationService,
            IBlockExecutor blockExecutor, IMinersManager minersManager, ILogger logger)
        {
            _chainService = chainService;
            _blockValidationService = blockValidationService;
            _blockExecutor = blockExecutor;
            _minersManager = minersManager;
            _logger = logger;

            _blockSet = new BlockSet();
            _stateFsm = new NodeStateFSM().Create();

            _logger = LogManager.GetLogger(nameof(BlockSynchronizer));

            _terminated = false;
            _executeNextBlock = true;

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
                    _logger?.Warn("Null headers or header list is empty.");
                    return;
                }

                var headers = inHeaders.Headers.OrderByDescending(h => h.Index).ToList();

                foreach (var blockHeader in headers)
                {
                    // Get previous block from the chain
                    var correspondingBlockHeader = await _blockChain.GetBlockByHeightAsync(blockHeader.Index - 1);

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

            MessageHub.Instance.Subscribe<DPoSStateChanged>(inState =>
            {
                MessageHub.Instance.Publish(inState.IsMining ? StateEvent.MiningStart : StateEvent.MiningEnd);
            });

            MessageHub.Instance.Subscribe<BlockMined>(inBlock =>
            {
                AddMinedBlock(inBlock.Block);
            });

            MessageHub.Instance.Subscribe<TerminationSignal>(signal =>
            {
                if (signal.Module == TerminatedModuleEnum.BlockSynchronizer)
                {
                    _terminated = true;
                    MessageHub.Instance.Publish(new TerminatedModule(TerminatedModuleEnum.BlockSynchronizer));
                }
            });
            
            MessageHub.Instance.Subscribe<BlockReceived>(async inBlock =>
            {
                if (inBlock.Block == null)
                    return;
                    
                _logger?.Debug($"Handling {inBlock.Block} - state {CurrentState}");
            
                CacheBlock(inBlock.Block);
            
                await TryExecuteNextCachedBlock();
            });
        }

        public void Init()
        {
            if (string.IsNullOrEmpty(ChainConfig.Instance?.ChainId))
                throw new InvalidOperationException("Chain id cannot be empty...");            
            
            if (NodeConfig.Instance?.ECKeyPair?.PublicKey == null)
                throw new InvalidOperationException("Node key pair cannot be empty...");
            
            try
            {
                _chainId = Hash.LoadBase58(ChainConfig.Instance.ChainId);
                _blockChain = _chainService.GetBlockChain(_chainId);
                _nodePubKey = NodeConfig.Instance.ECKeyPair.PublicKey.ToHex();
            
                Miners miners = _minersManager.GetMiners().Result;
            
                _currentMiners = new List<string>();

                foreach (var miner in miners.PublicKeys)
                {
                    _currentMiners.Add(miner);
                    _logger?.Debug($"Added a miner {miner}");
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
                _logger?.Error(e, "Error while initializing block sync.");
            }
        }

        private void BlockSetOnLibChanged(object sender, EventArgs e)
        {
            if (e is LibChangedArgs libChangedArgs)
            {
                CurrentLib = libChangedArgs.NewLib;
                
                MessageHub.Instance.Publish(new NewLibFound
                {
                    Height = libChangedArgs.NewLib.Index,
                    BlockHash = libChangedArgs.NewLib.BlockHash
                });
            }
        }

        /// <summary>
        /// If the block CurrentHeight + 1 on current fork existes, then execute it.
        /// </summary>
        private async Task TryExecuteNextCachedBlock()
        {
            if (!_executeNextBlock)
            {
                _executeNextBlock = true;
                return;
            }
            
            // no handling of blocks in other states than Catching/Caught (case where the node is busy).
            if (_stateFsm.CurrentState != NodeState.Catching && _stateFsm.CurrentState != NodeState.Caught)
            {
                IncorrectStateLog(nameof(TryExecuteNextCachedBlock));
                return;
            }

            if (_currentBlock != null)
            {
                _logger?.Debug("Current not null, returning");
                return;
            }

            IBlock next;
            lock (_blockCacheLock)
            {
                if (!_blockCache.Any())
                    return;

                // execute the block with the lowest index
                next = _blockCache.OrderBy(b => b.Index).FirstOrDefault();
                
                _blockCache.Remove(next);

                _currentBlock = next;
                
                _logger?.Debug($"Removed from cache : {next}");
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
            if (_terminated)
                return;
            
            if (block == null)
                throw new ArgumentNullException(nameof(block), "The block cannot be null");
            
            try
            {
                // Catching/Caught -> BlockValidating
                MessageHub.Instance.Publish(StateEvent.ValidBlockHeader);
                
                // todo check existence in database
            
                if (block.Index > HeadBlock.Index + 1)
                {
                    _logger?.Warn($"Future block {block}, current height {HeadBlock.Index} ");
                    MessageHub.Instance.Publish(StateEvent.InvalidBlock); // get back to Catching
                    return;
                }
                
                if (_blockSet.IsBlockReceived(block))
                {
                    _logger?.Warn($"Block already known {block}, current height {HeadBlock.Index} ");
                    MessageHub.Instance.Publish(StateEvent.InvalidBlock); // get back to Catching
                    return;
                }
                
                try
                {
                    _blockSet.PushBlock(block);
                }
                catch (UnlinkableBlockException)
                {
                    _logger?.Warn($"Block unlinkable {block}");
                    MessageHub.Instance.Publish(StateEvent.InvalidBlock); // get back to Catching
                    // todo event on unlinkable
                    return;
                }
            
                MessageHub.Instance.Publish(new BlockAccepted(block));

                _logger?.Trace($"Pushed {block}, current state {CurrentState}, current head {HeadBlock}");
                
                // At this point we're ready to execute the block
                // Here if we detect that we're out of sync with the current blockset head -> switch forks
                if (HeadBlock.BlockHash != _blockSet.CurrentHead.BlockHash && HeadBlock.BlockHash != _blockSet.CurrentHead.Previous) 
                {
                    // The SwitchFork method should handle the FSMs state, rollback current branch and execute the other branch.
                    // and the updates the current branch in the blockset
                    // it switches the FSM back to Catching/Caught so the state should be ok.
                    await TrySwitchFork();
                }
                else
                {
                    await HandleBlock(block);
                }
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Error while handling a block.");
            }
        }

        private async Task HandleBlock(IBlock block)
        {
            if (_stateFsm.CurrentState != NodeState.BlockValidating)
            {
                IncorrectStateLog(nameof(HandleBlock));
                return;
            }

            var blockValidationResult = await _blockValidationService.ValidateBlockAsync(block, await GetChainContextAsync());
            _logger?.Info($"Block validation result {block} - {blockValidationResult}");

            if (blockValidationResult.IsSuccess())
            {
                // BlockValidating -> BlockExecuting
                MessageHub.Instance.Publish(StateEvent.ValidBlock);
                await HandleValidBlock(block);
            }
            else
            {
                // BlockValidating -> Catching / Caught
                MessageHub.Instance.Publish(StateEvent.InvalidBlock);
                await HandleInvalidBlock(block, blockValidationResult);
                _blockSet.RemoveInvalidBlock(block);
            }
        }

        private async Task<BlockExecutionResult> HandleValidBlock(IBlock block)
        {
            if (_stateFsm.CurrentState != NodeState.BlockExecuting)
            {
                IncorrectStateLog(nameof(HandleValidBlock));
                return BlockExecutionResult.IncorrectNodeState;
            }

            var executionResult = await _blockExecutor.ExecuteBlock(block);

            _logger?.Trace($"Execution result of block {block} - {executionResult}");

            if (executionResult.CanExecuteAgain())
            {
                // todo side chain logic
                // BlockExecuting -> ExecutingLoop
                //MessageHub.Instance.Publish(StateEvent.StateNotUpdated);
                //await KeepExecutingBlocksOfHeight(block.Index);
                return BlockExecutionResult.InvalidSideChaiTransactionMerkleTreeRoot;
            }
            else if (executionResult.CannotExecute())
            {
                _executeNextBlock = false;
                _blockSet.RemoveInvalidBlock(block);
            }

            // if the current chain has just been extended and the block was successfully
            // executed we can change the synchronizers head.
            if (HeadBlock == _blockSet.CurrentHead.PreviousState)
                HeadBlock = _blockSet.CurrentHead;

            MessageHub.Instance.Publish(UpdateConsensus.Update);

            // BlockAppending -> Catching / Caught
            MessageHub.Instance.Publish(StateEvent.BlockAppended);
            
            MessageHub.Instance.Publish(new BlockExecuted(block));

            return BlockExecutionResult.Success;
        }

        private Task HandleInvalidBlock(IBlock block, BlockValidationResult blockValidationResult)
        {
            _logger?.Warn( $"Invalid block ({blockValidationResult}) {block}");

            MessageHub.Instance.Publish(new LockMining(false));

            // Handle the invalid blocks according to their validation results.
            if ((int) blockValidationResult < 100)
            {
                // todo probably wrong algo but the best is to add to cache
                CacheBlock(block);
            }

            return Task.CompletedTask;
        }
        
        /// <summary>
        /// Method called to switch the current branch to a longer one.
        /// </summary>
        private async Task TrySwitchFork()
        {
            // We should come from either Catching or Caught
            if (CurrentState != NodeState.BlockValidating)
                _logger?.Warn("Unexpected state...");

            if (HeadBlock == _blockSet.CurrentHead)
            {
                _logger?.Warn("Current head already the same as block set.");
                return;
            }
            
            // Get ourselves into the Reverting state 
            MessageHub.Instance.Publish(StateEvent.LongerChainDetected);
            
            // Dispose consensus (reported from previous logic)
            MessageHub.Instance.Publish(UpdateConsensus.Dispose);

            // CurrentHead to BlockSet.CurrentHead
            List<BlockState> toexec = _blockSet.GetBranch(HeadBlock, _blockSet.CurrentHead);
            toexec = toexec.OrderBy(b => b.Index).ToList();
                
            // todo switch the block set (update it's state to its longest branch)
            
            await _blockChain.RollbackToHeight(toexec.First().Index);

            // exec blocks one by one
            foreach (var block in toexec)
            {
                var executionResult = await _blockExecutor.ExecuteBlock(block.GetClonedBlock());
                _logger?.Trace($"Execution of {block} : {executionResult}");
            }

            HeadBlock = _blockSet.CurrentHead;

            RollBackTimes++;
                
            // Reverting -> Catching
            MessageHub.Instance.Publish(StateEvent.RollbackFinished);
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
                _blockSet.PushBlock(block, true);
                        
                // if the current chain has just been extended update
                if (HeadBlock == _blockSet.CurrentHead.PreviousState)
                {
                    HeadBlock = _blockSet.CurrentHead;
                    _logger?.Trace($"Pushed {block}, current head {HeadBlock}, current state {CurrentState}");
                }
                else
                {
                    _logger?.Warn($"Mined block did not extend current chain ! Pushed {block}, current head {HeadBlock}, current state {CurrentState}");
                }
            }
            catch (UnlinkableBlockException)
            {
                _logger?.Warn($"Block unlinkable {block}");
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
                    ((BlockHeader) await _blockChain.GetHeaderByHashAsync(chainContext.BlockHash)).Index;
            }

            return chainContext;
        }

        public IBlock GetBlockByHash(Hash blockHash)
        {
            return _blockSet.GetBlockByHash(blockHash) ?? _blockChain.GetBlockByHashAsync(blockHash).Result;
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

        /// <summary>
        /// Keep executing blocks of specific height until the NodeState changed.
        /// </summary>
//        private async Task KeepExecutingBlocksOfHeight(ulong height)
//        {
//            _logger?.Trace("Entered KeepExecutingBlocksOfHeight");
//
//            while (_stateFsm.CurrentState == NodeState.ExecutingLoop)
//            {
//                var blocks = _blockSet.GetBlocksByHeight(height).Where(b => _blockHeaderValidator.ValidateBlockHeaderAsync(b.Header).Result == BlockHeaderValidationResult.Success);
//
//                foreach (var block in blocks)
//                {
//                    var res = await _blockExecutor.ExecuteBlock(block);
//
//                    if (res.IsSuccess())
//                    {
//                        MessageHub.Instance.Publish(StateEvent.BlockAppended);
//                        MessageHub.Instance.Publish(new BlockExecuted(block));
//                    }
//
//                    if (new Random().Next(10000) % 1000 == 0)
//                    {
//                        _logger?.Trace($"Execution result == {res.ToString()}");
//                    }
//                }
//
//                if (_terminated)
//                {
//                    return;
//                }
//            }
//        }

        private void IncorrectStateLog(string methodName)
        {
            _logger?.Trace($"Incorrect fsm state: {_stateFsm.CurrentState.ToString()} in method {methodName}");
        }
    }
}