using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.ChainController.EventMessages;
using AElf.Common;
using AElf.Common.FSM;
using AElf.Configuration.Config.Chain;
using AElf.Kernel;
using AElf.Kernel.EventMessages;
using AElf.Kernel.Types.Common;
using AElf.Miner.EventMessages;
using AElf.Synchronization.BlockExecution;
using AElf.Synchronization.EventMessages;
using Easy.MessageHub;
using NLog;

namespace AElf.Synchronization.BlockSynchronization
{
    public class BlockSynchronizer : IBlockSynchronizer
    {
        private readonly IChainService _chainService;
        private readonly IBlockValidationService _blockValidationService;
        private readonly IBlockHeaderValidator _blockHeaderValidator;
        private readonly IBlockExecutor _blockExecutor;

        private IBlockChain _blockChain;

        private IBlockChain BlockChain => _blockChain ?? (_blockChain =
                                              _chainService.GetBlockChain(
                                                  Hash.LoadBase58(ChainConfig.Instance.ChainId)));

        private readonly ILogger _logger;

        private readonly FSM<NodeState> _stateFSM;

        private bool _terminated;

        private NodeState CurrentState => _stateFSM.CurrentState;

        private bool _executeNextBlock;

        public int RollBackTimes { get; private set; }

        private BlockState _currentBlock;
        private BlockState _currentLib;
        
        private readonly BlockSet _blockSet;

        public BlockSynchronizer(IChainService chainService, IBlockValidationService blockValidationService,
            IBlockExecutor blockExecutor, IBlockHeaderValidator blockHeaderValidator)
        {
            _chainService = chainService;
            _blockValidationService = blockValidationService;
            _blockExecutor = blockExecutor;
            _blockHeaderValidator = blockHeaderValidator;
            
            _blockSet = new BlockSet();

            _stateFSM = new NodeStateFSM().Create();

            _logger = LogManager.GetLogger(nameof(BlockSynchronizer));

            _terminated = false;
            _executeNextBlock = true;

            MessageHub.Instance.Subscribe<StateEvent>(e =>
            {
                _stateFSM.ProcessWithStateEvent(e);
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
                        await HandleNextValidBlock();
                        break;
                    
                    case NodeState.Caught:
                        await HandleNextValidBlock();
                        break;

                    case NodeState.ExecutingLoop:
                        // This node is free to mine a block during executing maybe-incorrect block again and again.
                        MessageHub.Instance.Publish(new LockMining(false));
                        break;
                    
                    case NodeState.GeneratingConsensusTx:
                        MessageHub.Instance.Publish(new LockMining(false));
                        break;
                }
            });

            MessageHub.Instance.Subscribe<LeavingState>(inState =>
            {
                if (inState.NodeState.ShouldUnlockMiningWhenLeaving())
                {
                    MessageHub.Instance.Publish(new LockMining(false));
                }
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
                    var correspondingBlockHeader = await BlockChain.GetBlockByHeightAsync(blockHeader.Index - 1);

                    // If the hash of this previous block corresponds to "previous block hash" of the current header
                    // the link has been found
                    if (correspondingBlockHeader.BlockHashToHex == blockHeader.PreviousBlockHash.DumpHex())
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
                // Update DPoS process.
                AddMinedBlock(inBlock.Block);
            });
            
            MessageHub.Instance.Subscribe<BlockMinedAndStored>(inBlock =>
            {
                // Update DPoS process.
                MessageHub.Instance.Publish(UpdateConsensus.Update);
                HandleMinedAndStoredBlock(inBlock.Block);
            });

            MessageHub.Instance.Subscribe<TerminationSignal>(signal =>
            {
                if (signal.Module == TerminatedModuleEnum.BlockSynchronizer)
                {
                    _terminated = true;
                    MessageHub.Instance.Publish(new TerminatedModule(TerminatedModuleEnum.BlockSynchronizer));
                }
            });
        }

        public void Init()
        {
            var height = BlockChain.GetCurrentBlockHeightAsync().Result;
            var currentBlock = BlockChain.GetBlockByHeightAsync(height).Result as Block;
            _currentBlock = _blockSet.Init(currentBlock);
        }

        /// <summary>
        /// Method called to switch the current branch to a longer one.
        /// </summary>
        private async Task SwitchFork()
        {
            // We should come from either Catching or Caught
            if (CurrentState != NodeState.Catching && CurrentState != NodeState.Caught)
                _logger?.Warn("Unexpected state...");

            if (_currentBlock == _blockSet.CurrentHead)
            {
                _logger?.Warn("Current head already the same as block set.");
                return;
            }
            
            // Get ourselves into the Reverting state 
            MessageHub.Instance.Publish(StateEvent.LongerChainDetected);
            
            // Dispose consensus (reported from previous logic)
            MessageHub.Instance.Publish(UpdateConsensus.Dispose);

            // CurrentHead to BlockSet.CurrentHead
            List<BlockState> toexec = _blockSet.GetBranch(_currentBlock, _blockSet.CurrentHead);
            toexec = toexec.OrderBy(b => b.Index).ToList();
                
            // todo switch the block set (update it's state to its longest branch)
            
            await BlockChain.RollbackToHeight(toexec.First().Index);

            // exec blocks one by one
            foreach (var block in toexec)
            {
                var executionResult = await _blockExecutor.ExecuteBlock(block.GetClonedBlock());
                _logger?.Trace($"Execution of {block} : {executionResult}");
            }

            // Reverting -> Catching
            MessageHub.Instance.Publish(StateEvent.RollbackFinished);
        }

        /// <summary>
        /// Called when receiving a block through the network -or- NodeState.Catching/NodeState.Caught
        /// state change has triggered the <see cref="HandleNextValidBlock"/> method. cf. Next valid block comments.
        /// When coming from <see cref="HandleNextValidBlock"/> the block is *already* in the block set. 
        /// </summary>
        /// <param name="block"></param>
        public async Task ReceiveBlock(IBlock block)
        {
            if (_terminated)
                return;
            
            if (block.Index > _currentBlock.Index + 1)
            {
                _logger?.Warn($"Future block {block}, current height {_currentBlock.Index} ");
                return;
            }

            // AlreadyExecuted - should ont happen (maybe related to previous blocks) -> ??.
            // Branched - normal situation, just receive a block from a fork.
            var validationResult = await _blockHeaderValidator.ValidateBlockHeaderAsync(block.Header);

            _logger?.Trace($"Header validated ({validationResult.ToString()}) {block}");
            
            // Concerning the block set these two validation results are not a problem.
            if (validationResult == BlockHeaderValidationResult.Success || validationResult == BlockHeaderValidationResult.Branched)
            {
                // Add to a one of the branches if not already in the blocks
                // can already be here if we come from "HandleNextValidBlock"
                if (!_blockSet.IsBlockReceived(block))
                {
                    try
                    {
                        _blockSet.PushBlock(block);
                        
                        // if the current chain has just been extended update
                        if (_currentBlock == _blockSet.CurrentHead.PreviousState)
                            _currentBlock = _blockSet.CurrentHead;
                        
                        _logger?.Trace($"Pushed {block}, current state {CurrentState}");
                    }
                    catch (UnlinkableBlockException e)
                    {
                        _logger?.Warn($"Block unlinkable {block}");
                        return;
                    }
                    
                    MessageHub.Instance.Publish(new BlockAccepted(block));
                }
            }

            // This guard is to handle the cases where the node is Executing, Appending, Validating...
            if (CurrentState != NodeState.Catching && CurrentState != NodeState.Caught)
            {
                _logger?.Trace($"In other state: {CurrentState}");
                return;
            }

            // At this point we're ready to execute another block
            
            // Here if we detect that we're out of sync witht the current blockset head -> switch forks
            if (_currentBlock != _blockSet.CurrentHead) 
            {
                // The SwitchFork method should handle the FSMs state, rollback current branch and execute the other branch.
                // and the updates the current branch in the blockset
                // it switches the FSM back to Catching/Caught so the state should be ok.
                _logger?.Trace("About to switch fork");
                await SwitchFork();
            }
            else
            {
                if (validationResult == BlockHeaderValidationResult.Success)
                {
                    _logger?.Trace($"Handling {block}");
                    
                    // Catching/Caught -> BlockValidating
                    MessageHub.Instance.Publish(StateEvent.ValidBlockHeader);
                    await HandleBlock(block);
                }
            }
        }

        /// <summary>
        /// If the block CurrentHeight + 1 on current fork existes, then execute it.
        /// </summary>
        /// <returns></returns>
        private async Task HandleNextValidBlock()
        {
            if (!_executeNextBlock)
            {
                _executeNextBlock = true;
                return;
            }
            
            // There should be no handling of blocks in other states than Catching/Caught.
            if (_stateFSM.CurrentState != NodeState.Catching && _stateFSM.CurrentState != NodeState.Caught)
            {
                IncorrectStateLog(nameof(HandleNextValidBlock));
                return;
            }

            // todo only get one block from here (the one with IsCurrentFork).
            var currentBlockHeight = await BlockChain.GetCurrentBlockHeightAsync(); // todo get from this._currentBlock
            var nextBlock = _blockSet.GetBlocksByHeight(currentBlockHeight + 1).FirstOrDefault(b => b.IsInCurrentBranch);

            if (nextBlock == null)
            {
                _logger?.Trace("No more blocks to receive.");
                return;
            }
            
            await ReceiveBlock(nextBlock.GetClonedBlock());
            
            if (_stateFSM.CurrentState == NodeState.Catching || _stateFSM.CurrentState == NodeState.Caught)
                _logger?.Warn("Executed a block, but state still Cathcing or Reverting");
        }

        private async Task HandleBlock(IBlock block)
        {
            if (_stateFSM.CurrentState != NodeState.BlockValidating)
            {
                IncorrectStateLog(nameof(HandleBlock));
                return;
            }

            var blockValidationResult =
                await _blockValidationService.ValidateBlockAsync(block, await GetChainContextAsync());

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
            }
        }

        private async Task<BlockExecutionResult> HandleValidBlock(IBlock block)
        {
            _logger?.Info($"Valid block {block.BlockHashToHex}. Height: *{block.Index}*");

            if (_stateFSM.CurrentState != NodeState.BlockExecuting)
            {
                IncorrectStateLog(nameof(HandleValidBlock));
                return BlockExecutionResult.IncorrectNodeState;
            }

            var executionResult = await _blockExecutor.ExecuteBlock(block);

            _logger?.Trace($"Execution result of block {block.BlockHashToHex}: {executionResult}. Height *{block.Index}*");

            if (executionResult.CanExecuteAgain())
            {
                // BlockExecuting -> ExecutingLoop
                MessageHub.Instance.Publish(StateEvent.StateNotUpdated);
                //await KeepExecutingBlocksOfHeight(block.Index); todo 
                return BlockExecutionResult.InvalidSideChainInfo;
            }

            if (executionResult.CannotExecute())
            {
                _executeNextBlock = false;
            }

            // Update the consensus information.
            MessageHub.Instance.Publish(UpdateConsensus.Update);
            
            //_logger.Trace(111111111111);

            // BlockAppending -> Catching / Caught
            MessageHub.Instance.Publish(StateEvent.BlockAppended);
            
            //_logger.Trace(222222221111);
            
            MessageHub.Instance.Publish(new BlockExecuted(block));

            return BlockExecutionResult.Success;
        }

        private Task HandleInvalidBlock(IBlock block, BlockValidationResult blockValidationResult)
        {
            _logger?.Warn(
                $"Invalid block {block.BlockHashToHex} : {blockValidationResult.ToString()}. Height: *{block.Index}*");

            MessageHub.Instance.Publish(new LockMining(false));

            // Handle the invalid blocks according to their validation results.
            if ((int) blockValidationResult < 100)
            {
                _blockSet.PushBlock(block);
            }

            return Task.CompletedTask;
        }

        private void AddMinedBlock(IBlock block)
        {
            _blockSet.PushBlock(block); // todo review
            SetKeepHeight();
        }
        
        private void HandleMinedAndStoredBlock(IBlock block)
        {
            SetKeepHeight();
        }

        private void SetKeepHeight()
        {
            // We can say the "initial sync" is finished, set KeepHeight to a specific number
            if (_blockSet.KeepHeight == ulong.MaxValue)
            {
                _logger?.Trace($"Set the limit of the branched blocks cache in block set to {GlobalConfig.BlockCacheLimit}.");
                _blockSet.KeepHeight = GlobalConfig.BlockCacheLimit;
            }
        }

        private async Task RollbackToHeight(ulong targetHeight, ulong currentHeight)
        {
            // Stop all the mining processes.
            MessageHub.Instance.Publish(UpdateConsensus.Dispose);

            await BlockChain.RollbackToHeight(targetHeight - 1);

            // Revert block set.
            //_blockSet.InformRollback(targetHeight, currentHeight);

            // Reverting -> Catching
            MessageHub.Instance.Publish(StateEvent.RollbackFinished);
        }

        private async Task<IChainContext> GetChainContextAsync()
        {
            var chainId = Hash.LoadBase58(ChainConfig.Instance.ChainId);
            var blockchain = _chainService.GetBlockChain(chainId);
            IChainContext chainContext = new ChainContext
            {
                ChainId = chainId,
                BlockHash = await blockchain.GetCurrentBlockHashAsync()
            };

            if (chainContext.BlockHash != Hash.Genesis && chainContext.BlockHash != null)
            {
                chainContext.BlockHeight =
                    ((BlockHeader) await blockchain.GetHeaderByHashAsync(chainContext.BlockHash)).Index;
            }

            return chainContext;
        }

        public IBlock GetBlockByHash(Hash blockHash)
        {
            return _blockSet.GetBlockByHash(blockHash) ?? BlockChain.GetBlockByHashAsync(blockHash).Result;
        }

        public async Task<BlockHeaderList> GetBlockHeaderList(ulong index, int count)
        {
            var blockHeaderList = new BlockHeaderList();
            for (var i = index; i > index - (ulong) count; i--)
            {
                var block = await BlockChain.GetBlockByHeightAsync(i);
                blockHeaderList.Headers.Add(block.Header);
            }

            return blockHeaderList;
        }

        /// <summary>
        /// Keep executing blocks of specific height until the NodeState changed.
        /// </summary>
        /// <param name="height"></param>
        /// <returns></returns>
//        private async Task KeepExecutingBlocksOfHeight(ulong height)
//        {
//            _logger?.Trace("Entered KeepExecutingBlocksOfHeight");
//
//            while (_stateFSM.CurrentState == NodeState.ExecutingLoop)
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
//                        
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
            _logger?.Trace(
                $"Incorrect fsm state: {_stateFSM.CurrentState.ToString()} in method {methodName}");
        }
    }
}