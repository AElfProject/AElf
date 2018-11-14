using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.ChainController.EventMessages;
using AElf.Common;
using AElf.Common.FSM;
using AElf.Configuration.Config.Chain;
using AElf.Kernel;
using AElf.Kernel.Types;
using AElf.Kernel.Types.Common;
using AElf.Miner.EventMessages;
using AElf.Synchronization.BlockExecution;
using AElf.Synchronization.EventMessages;
using Easy.MessageHub;
using NLog;

// ReSharper disable once CheckNamespace
namespace AElf.Synchronization.BlockSynchronization
{
    // ReSharper disable InconsistentNaming
    public class BlockSynchronizer : IBlockSynchronizer
    {
        // Some dependencies.
        private readonly IChainService _chainService;
        private readonly IBlockValidationService _blockValidationService;
        private readonly IBlockHeaderValidator _blockHeaderValidator;
        private readonly IBlockExecutor _blockExecutor;
        private readonly IBlockSet _blockSet;

        private IBlockChain _blockChain;

        private IBlockChain BlockChain => _blockChain ?? (_blockChain =
                                              _chainService.GetBlockChain(
                                                  Hash.LoadHex(ChainConfig.Instance.ChainId)));

        private readonly ILogger _logger;

        private readonly FSM<NodeState> _stateFSM;

        private static int _flag;

        private static bool _executingRemainingBlocks;

        private static bool _terminated;

        private static ulong _heightOfUnlinkableBlock;

        private static bool _isExecuting;

        public BlockSynchronizer(IChainService chainService, IBlockValidationService blockValidationService,
            IBlockExecutor blockExecutor, IBlockSet blockSet, IBlockHeaderValidator blockHeaderValidator)
        {
            _chainService = chainService;
            _blockValidationService = blockValidationService;
            _blockExecutor = blockExecutor;
            _blockSet = blockSet;
            _blockHeaderValidator = blockHeaderValidator;

            _stateFSM = new NodeStateFSM().Create();
            _stateFSM.CurrentState = NodeState.Catching;

            _logger = LogManager.GetLogger(nameof(BlockSynchronizer));

            _terminated = false;

            MessageHub.Instance.Subscribe<StateEvent>(e => { _stateFSM.ProcessWithStateEvent(e); });

            MessageHub.Instance.Subscribe<EnteringCatchingOrCaughtState>(async e => { await ReceiveNextValidBlock(); });

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
                if (inState.IsMining)
                {
                    MessageHub.Instance.Publish(StateEvent.MiningStart);
                }
                else
                {
                    MessageHub.Instance.Publish(StateEvent.MiningEnd);
                }
            });
            
            MessageHub.Instance.Subscribe<BlockMined>(inBlock =>
            {
                // Update DPoS process.
                MessageHub.Instance.Publish(UpdateConsensus.Update);
                AddMinedBlock(inBlock.Block); 
            });
            
            MessageHub.Instance.Subscribe<ExecutionStateChanged>(inState => { _isExecuting = inState.IsExecuting; });

            MessageHub.Instance.Subscribe<TerminationSignal>(signal =>
            {
                if (signal.Module == TerminatedModuleEnum.BlockSynchronizer)
                {
                    _terminated = true;
                    MessageHub.Instance.Publish(new TerminatedModule(TerminatedModuleEnum.BlockSynchronizer));
                }
            });
        }

        /// <summary>
        /// The entrance of block syncing.
        /// First step is to validate block header of this block.
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        public async Task ReceiveBlock(IBlock block)
        {
            if (_terminated)
            {
                return;
            }

            if (!_blockSet.IsBlockReceived(block.GetHash(), block.Index))
            {
                _blockSet.AddBlock(block);
            }

            var blockHeaderValidationResult =
                await _blockHeaderValidator.ValidateBlockHeaderAsync(block.Header);
            
            _logger?.Trace($"BlockHeader validation result: {blockHeaderValidationResult.ToString()} - {block.BlockHashToHex}");

            if (blockHeaderValidationResult == BlockHeaderValidationResult.Success)
            {
                // Catching -> BlockValidating
                // Caught -> BlockValidating
                MessageHub.Instance.Publish(StateEvent.ValidBlockHeader);
                await HandleBlock(block);
            }

            if (blockHeaderValidationResult == BlockHeaderValidationResult.Unlinkable)
            {
                MessageHub.Instance.Publish(new UnlinkableHeader(block.Header));
            }

            /*var currentBlockHeight = await BlockChain.GetCurrentBlockHeightAsync();

            _latestHandledValidBlock = Math.Max(_latestHandledValidBlock, currentBlockHeight);

            if (block.Index > _latestHandledValidBlock + 1)
            {
                if (_firstFutureBlockHeight == 0)
                    _firstFutureBlockHeight = block.Index;

                _blockSet.AddBlock(block);

                _logger?.Trace($"Added block {block.BlockHashToHex} to block cache cause this is a future block.");

                if (block.Index >= currentBlockHeight + GlobalConfig.ForkDetectionLength)
                {
                    _heightOfUnlinkableBlock = block.Index;
                    await ReviewBlockSet();
                }
            }

            if (block.Index == _latestHandledValidBlock + 1)
            {
                _nextBlock = block;
            }

            */
        }

        public async Task ReceiveNextValidBlock()
        {
            if (_stateFSM.CurrentState != NodeState.Catching && _stateFSM.CurrentState != NodeState.Caught)
            {
                IncorrectStateLog(nameof(ReceiveNextValidBlock));
                return;
            }
            
            var currentBlockHeight = await BlockChain.GetCurrentBlockHeightAsync();
            var nextBlocks = _blockSet.GetBlocksByHeight(currentBlockHeight + 1);
            foreach (var nextBlock in nextBlocks)
            {
                await ReceiveBlock(nextBlock);
                if (_stateFSM.CurrentState != NodeState.Catching && _stateFSM.CurrentState != NodeState.Caught)
                {
                    break;
                }
            }
        }

        private async Task HandleBlock(IBlock block)
        {
            if (_stateFSM.CurrentState != NodeState.BlockValidating)
            {
                IncorrectStateLog(nameof(HandleBlock));
                return;
            }
            
            _logger?.Trace("Entered HandleBlock");
            
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
                await HandleInvalidBlock(block, blockValidationResult);
            }

            /*_logger?.Trace("Trying to enter HandleBlock");
            var lockWasTaken = Interlocked.CompareExchange(ref _flag, 1, 0) == 0;
            if (lockWasTaken)
            {
                _logger?.Trace("Entered HandleBlock");

                var blockValidationResult =
                    await _blockValidationService.ValidateBlockAsync(block, await GetChainContextAsync());

                if (blockValidationResult.IsSuccess())
                {
                    await HandleValidBlock(block);
                }

                await HandleInvalidBlock(block, blockValidationResult);
            }*/
        }

        private async Task<BlockExecutionResult> HandleValidBlock(IBlock block)
        {
            if (_stateFSM.CurrentState != NodeState.BlockExecuting)
            {
                IncorrectStateLog(nameof(HandleValidBlock));
                return BlockExecutionResult.IncorrectNodeState;
            }
            
            var executionResult = await _blockExecutor.ExecuteBlock(block);

            _logger?.Trace($"Execution result of block {block.BlockHashToHex}: {executionResult}. Height *{block.Index}*");

            if (executionResult.CanExecuteAgain())
            {
                await KeepExecutingBlocksOfHeight(block.Index);
            }
            
            _blockSet.Tell(block);

            // Update the consensus information.
            MessageHub.Instance.Publish(UpdateConsensus.Update);
            
            // Notify the network layer the block has been executed.
            MessageHub.Instance.Publish(new BlockExecuted(block));
            
            // BlockAppending -> Catching / Caught
            MessageHub.Instance.Publish(StateEvent.BlockAppended);
            
            return BlockExecutionResult.Success;

            /*_latestHandledValidBlock = block.Index;

            _logger?.Trace($"Valid block {block.BlockHashToHex}. Height: **{block.Index}**");

            _blockSet.AddBlock(block);
            if (executionResult.NeedToRollback())
            {
                // Need to rollback one block:
                await BlockChain.RollbackOneBlock();
                _blockSet.InformRollback(block.Index, block.Index);

                // Basically re-sync the block of specific height.
                await ExecuteRemainingBlocks(block.Index);

                return executionResult;
            }

            if (executionResult.CannotExecute())
            {
                _logger?.Trace($"Cannot execute block {block.BlockHashToHex} of height {block.Index}");
                return executionResult;
            }

            if (executionResult.CanExecuteAgain())
            {
                // No need to rollback:
                // Receive again to execute the same block.

                var currentBlockHash = await BlockChain.GetCurrentBlockHashAsync();
                if (_stateFSM.CurrentState.AsMiner() && !_executingRemainingBlocks)
                {
                    Thread.Sleep(200);
                    MessageHub.Instance.Publish(new LockMining(false));
                    BlockExecutionResult reExecutionResult1;
                    do
                    {
                        var reValidationResult = await _blockValidationService.ExecutingAgain(true)
                            .ValidateBlockAsync(block, await GetChainContextAsync());

                        if (reValidationResult.IsFailed())
                        {
                            break;
                        }

                        reExecutionResult1 = await _blockExecutor.ExecuteBlock(block);
                        _logger?.Trace($"Block execution result: {reExecutionResult1}.");

                        if (_blockSet.MultipleLinkableBlocksInOneIndex(block.Index, currentBlockHash.DumpHex()))
                        {
                            Thread.VolatileWrite(ref _flag, 0);
                            return reExecutionResult1;
                        }
                    } while (reExecutionResult1.CanExecuteAgain() && !_miningStarted);

                    Thread.VolatileWrite(ref _flag, 0);
                    return executionResult;
                }

                BlockExecutionResult reExecutionResult2;
                do
                {
                    Thread.Sleep(100);
                    var reValidationResult = await _blockValidationService.ExecutingAgain(true)
                        .ValidateBlockAsync(block, await GetChainContextAsync());

                    if (reValidationResult.IsFailed())
                    {
                        break;
                    }

                    reExecutionResult2 = await _blockExecutor.ExecuteBlock(block);
                    _logger?.Trace($"Block execution result: {reExecutionResult2}.");
                    if (_blockSet.MultipleLinkableBlocksInOneIndex(block.Index, currentBlockHash.DumpHex()))
                    {
                        Thread.VolatileWrite(ref _flag, 0);
                        return reExecutionResult2;
                    }
                } while (reExecutionResult2.CanExecuteAgain());
            }

            _blockSet.Tell(block);

            // Update the consensus information.
            MessageHub.Instance.Publish(UpdateConsensus.Update);

            Thread.VolatileWrite(ref _flag, 0);

            // Notify the network layer the block has been executed.
            MessageHub.Instance.Publish(new BlockExecuted(block));

            // In case of this synchronization run so long of time.
            if (_nextBlock?.Header != null && _nextBlock.Index == block.Index + 1)
            {
                await ReceiveBlock(_nextBlock);
            }

            // Sync future blocks.
            if (block.Index + 1 == _firstFutureBlockHeight)
            {
                await ExecuteRemainingBlocks(_firstFutureBlockHeight);
            }

            if (_heightBeforeRollback != 0)
            {
                if (block.Index >= _heightBeforeRollback)
                {
                    _heightBeforeRollback = 0;
                    _heightOfUnlinkableBlock = 0;
                    MessageHub.Instance.Publish(new CatchingUpAfterRollback(false));
                }
                else
                {
                    MessageHub.Instance.Publish(new CatchingUpAfterRollback(true));
                }
            }

            return BlockExecutionResult.Success;*/
        }

        private async Task HandleInvalidBlock(IBlock block, BlockValidationResult blockValidationResult)
        {
            Thread.VolatileWrite(ref _flag, 0);

            _logger?.Warn(
                $"Invalid block {block.BlockHashToHex} : {blockValidationResult.ToString()}. Height: **{block.Index}**");

            MessageHub.Instance.Publish(new LockMining(false));

            // Handle the invalid blocks according to their validation results.
            if ((int) blockValidationResult < 100)
            {
                _blockSet.AddBlock(block);
            }

            if (blockValidationResult == BlockValidationResult.Unlinkable)
            {
                _heightOfUnlinkableBlock = block.Index;

                _logger?.Warn("Received unlinkable block.");

                MessageHub.Instance.Publish(new UnlinkableHeader(block.Header));
            }

            // Received blocks from branched chain.
            if (blockValidationResult == BlockValidationResult.BranchedBlock)
            {
                _logger?.Warn("Received a block from branched chain.");

                await ReviewBlockSet();
            }

            // A weird situation.
            if (blockValidationResult == BlockValidationResult.Pending)
            {
                _heightOfUnlinkableBlock = block.Index;

                var currentHeight = await BlockChain.GetCurrentBlockHeightAsync();

                await ExecuteRemainingBlocks(currentHeight + 1);
            }
        }
        
        public void AddMinedBlock(IBlock block)
        {
            _blockSet.Tell(block);

            // We can say the "initial sync" is finished, set KeepHeight to a specific number
            if (_blockSet.KeepHeight == ulong.MaxValue)
            {
                _logger?.Trace(
                    $"Set the limit of the branched blocks cache in block set to {GlobalConfig.BlockCacheLimit}.");
                _blockSet.KeepHeight = GlobalConfig.BlockCacheLimit;
            }
        }

        private async Task ReviewBlockSet()
        {
            if (_heightOfUnlinkableBlock == 0 || _isExecuting)
            {
                return;
            }

            // In case of the block set exists blocks that should be valid but didn't executed yet.
            var currentHeight = await BlockChain.GetCurrentBlockHeightAsync();

            if (BlockSet.MaxHeight.HasValue && BlockSet.MaxHeight < currentHeight + GlobalConfig.ForkDetectionLength)
            {
                return;
            }

            // Detect longest chain and switch.
            var forkHeight = _blockSet.AnyLongerValidChain(currentHeight - GlobalConfig.ForkDetectionLength);
            // Execute next block.
            if (forkHeight == ulong.MaxValue)
            {
                await ExecuteRemainingBlocks(currentHeight + 1);
            }
            else if (forkHeight != 0)
            {
                await RollbackToHeight(forkHeight, currentHeight - GlobalConfig.ForkDetectionLength);
            }
        }

        private async Task RollbackToHeight(ulong targetHeight, ulong currentHeight)
        {
            await BlockChain.RollbackToHeight(targetHeight - 1);
            _blockSet.InformRollback(targetHeight, currentHeight);
            await ExecuteRemainingBlocks(targetHeight);
        }
        
        public async Task ExecuteRemainingBlocks(ulong targetHeight)
        {
            _executingRemainingBlocks = true;
            // Find new blocks from block set to execute
            var blocks = _blockSet.GetBlocksByHeight(targetHeight);
            ulong i = 0;
            var currentBlockHash = await BlockChain.GetCurrentBlockHashAsync();
            while (blocks != null &&
                   blocks.Any(b => b.Header.PreviousBlockHash.DumpHex() == currentBlockHash.DumpHex()))
            {
                _logger?.Trace($"Will get block of height {targetHeight + i} from block set to " +
                               $"execute - {blocks.Count} blocks.");

                i++;
                foreach (var block in blocks)
                {
                    var executionResult = await HandleValidBlock(block);
                    if (executionResult.IsFailed())
                    {
                        _executingRemainingBlocks = false;
                        return;
                    }
                }

                blocks = _blockSet.GetBlocksByHeight(targetHeight + i);
                currentBlockHash = await BlockChain.GetCurrentBlockHashAsync();
            }

            _executingRemainingBlocks = false;
        }
        
        private async Task<IChainContext> GetChainContextAsync()
        {
            var chainId = Hash.LoadHex(ChainConfig.Instance.ChainId);
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

        private async Task KeepExecutingBlocksOfHeight(ulong height)
        {
            while (_stateFSM.CurrentState == NodeState.ExecutingLoop)
            {
                var blocks = _blockSet.GetBlocksByHeight(height).Where(b =>
                    _blockHeaderValidator.ValidateBlockHeaderAsync(b.Header).Result == BlockHeaderValidationResult.Success);
                foreach (var block in blocks)
                {
                    await _blockExecutor.ExecuteBlock(block);
                    Thread.Sleep(200);
                }
            }
        }

        private void IncorrectStateLog(string methodName)
        {
            _logger?.Trace($"Incorrect fsm state: {_stateFSM.CurrentState.ToString()} in method {methodName}");
        }
    }
}