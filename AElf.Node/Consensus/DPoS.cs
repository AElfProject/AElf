using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.ChainController.EventMessages;
using AElf.Common;
using AElf.Common.FSM;
using AElf.Configuration;
using AElf.Configuration.Config.Chain;
using AElf.Configuration.Config.Consensus;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Consensus;
using AElf.Kernel.EventMessages;
using AElf.Kernel.Managers;
using AElf.Miner.Miner;
using AElf.Node;
using AElf.Types.CSharp;
using Easy.MessageHub;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using NLog;
using AElf.Miner.TxMemPool;
using AElf.Kernel.Storages;
using AElf.Kernel.Types.Common;
using AElf.Synchronization.EventMessages;
using Base58Check;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel.Node
{
    // ReSharper disable InconsistentNaming
    public class DPoS : IConsensus
    {
        /// <summary>
        /// Actually store the round number of DPoS processing.
        /// </summary>
        private ulong ConsensusMemory { get; set; }

        private static IDisposable ConsensusDisposable { get; set; }

        private bool _consensusInitialized;

        private readonly ITxHub _txHub;
        private readonly IMiner _miner;
        private readonly IChainService _chainService;

        private IBlockChain _blockChain;

        private IBlockChain BlockChain => _blockChain ?? (_blockChain =
                                              _chainService.GetBlockChain(
                                                  Hash.LoadByteArray(ChainConfig.Instance.ChainId.DecodeBase58())));

        private readonly ILogger _logger;

        private readonly AElfDPoSHelper _helper;

        private static int _lockNumber;

        private NodeState CurrentState { get; set; } = NodeState.Catching;

        /// <summary>
        /// In Value and Out Value.
        /// </summary>
        private readonly Stack<Hash> _consensusData = new Stack<Hash>();

        private readonly ECKeyPair _nodeKey;
        private readonly Address _nodeAddress;

        private readonly Hash _chainId;

        public Address ContractAddress => ContractHelpers.GetConsensusContractAddress(Hash.LoadBase58(ChainConfig.Instance.ChainId));

        private readonly IMinersManager _minersManager;

        private static int _flag;

        private static bool _prepareTerminated;

        private static bool _terminated;

        private AElfDPoSObserver AElfDPoSObserver => new AElfDPoSObserver(MiningWithInitializingAElfDPoSInformation,
            MiningWithPublishingOutValueAndSignature, PublishInValue, MiningWithUpdatingAElfDPoSInformation);

        public DPoS(ITxHub txHub, IMiner miner, IChainService chainService, IMinersManager minersManager,
            AElfDPoSHelper helper)
        {
            _nodeKey = NodeConfig.Instance.ECKeyPair;
            _chainId = Hash.LoadByteArray(ChainConfig.Instance.ChainId.DecodeBase58());
            _nodeAddress = Address.FromPublicKey(_chainId.DumpByteArray(), _nodeKey.PublicKey);
            
            _txHub = txHub;
            _miner = miner;
            _chainService = chainService;
            _minersManager = minersManager;
            _helper = helper;
            _prepareTerminated = false;
            _terminated = false;

            _logger = LogManager.GetLogger(nameof(DPoS));

            var count = MinersConfig.Instance.Producers.Count;

            GlobalConfig.BlockProducerNumber = count;
            GlobalConfig.BlockNumberOfEachRound = count + 1;

            _logger?.Info("Block Producer nodes count:" + GlobalConfig.BlockProducerNumber);
            _logger?.Info("Blocks of one round:" + GlobalConfig.BlockNumberOfEachRound);

            if (GlobalConfig.BlockProducerNumber == 1 && NodeConfig.Instance.IsMiner)
            {
                AElfDPoSObserver.RecoverMining();
            }

            MessageHub.Instance.Subscribe<UpdateConsensus>(async option =>
            {
                if (option == UpdateConsensus.Update)
                {
                    _logger?.Trace("UpdateConsensus - Update");
                    await UpdateConsensusEventList();
                }

                if (option == UpdateConsensus.Dispose)
                {
                    _logger?.Trace("UpdateConsensus - Dispose");
                    DisposeConsensusList();
                }
            });

            MessageHub.Instance.Subscribe<LockMining>(inState =>
            {
                if (inState.Lock)
                {
                    IncrementLockNumber();
                }
                else
                {
                    DecrementLockNumber();
                }
            });

            MessageHub.Instance.Subscribe<TerminationSignal>(signal =>
            {
                if (signal.Module == TerminatedModuleEnum.Mining)
                {
                    _prepareTerminated = true;
                }
            });

            MessageHub.Instance.Subscribe<FSMStateChanged>(inState => { CurrentState = inState.CurrentState; });
        }

        private Miners Miners => _minersManager.GetMiners().Result;

        public async Task Start()
        {
            // Consensus information already generated.
            if (ConsensusDisposable != null)
            {
                return;
            }

            if (_consensusInitialized)
                return;

            _consensusInitialized = true;

            // Check whether this node contained BP list.
            if (!Miners.Nodes.Contains(_nodeAddress))
            {
                return;
            }

            if (!await _minersManager.IsMinersInDatabase())
            {
                ConsensusDisposable = AElfDPoSObserver.Initialization();
                return;
            }

            _helper.SyncMiningInterval();

            if (_helper.CanRecoverDPoSInformation())
            {
                ConsensusDisposable = AElfDPoSObserver.RecoverMining();
            }
        }

        public void DisposeConsensusList()
        {
            ConsensusDisposable?.Dispose();
            _logger?.Trace("Mining stopped. Disposed previous consensus observables list.");
        }

        public void IncrementLockNumber()
        {
            Interlocked.Add(ref _lockNumber, 1);
            _logger?.Trace($"Lock number increment: {_lockNumber}");
        }

        public void DecrementLockNumber()
        {
            if (_lockNumber <= 0)
            {
                return;
            }

            Interlocked.Add(ref _lockNumber, -1);
            _logger?.Trace($"Lock number decrement: {_lockNumber}");
        }

        private async Task<IBlock> Mine()
        {
            try
            {
                var block = await _miner.Mine();

                if (_prepareTerminated)
                {
                    _terminated = true;
                    MessageHub.Instance.Publish(new TerminatedModule(TerminatedModuleEnum.Mining));
                }

                return block;
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Exception while mining.");
                return null;
            }
        }

        private async Task<Transaction> GenerateTransactionAsync(string methodName, List<byte[]> parameters)
        {
            try
            {
                _logger?.Trace("Entered generating tx.");
                var bn = await BlockChain.GetCurrentBlockHeightAsync();
                bn = bn > 4 ? bn - 4 : 0;
                var bh = bn == 0 ? Hash.Genesis : (await BlockChain.GetHeaderByHeightAsync(bn)).GetHash();
                var bhPref = bh.Value.Where((x, i) => i < 4).ToArray();
                
                var tx = new Transaction
                {
                    From = _nodeAddress,
                    To = ContractAddress,
                    RefBlockNumber = bn,
                    RefBlockPrefix = ByteString.CopyFrom(bhPref),
                    MethodName = methodName,
                    Type = TransactionType.DposTransaction,
                    Params = ByteString.CopyFrom(ParamsPacker.Pack(parameters.Select(p => (object) p).ToArray()))
                };
                
                var signer = new ECSigner();
                var signature = signer.Sign(_nodeKey, tx.GetHash().DumpByteArray());
                tx.Sigs.Add(ByteString.CopyFrom(signature.SigBytes));

                _logger?.Trace("Leaving generating tx.");

                MessageHub.Instance.Publish(StateEvent.ConsensusTxGenerated);

                return tx;
            }
            catch (Exception e)
            {
                _logger?.Trace(e, "Error while during generating DPoS tx.");
            }

            return null;
        }

        /// <summary>
        /// Related tx has 4 params:
        /// 1. Miners list
        /// 2. Information of first rounds
        /// 3. Mining interval
        /// 4. Log level
        /// </summary>
        /// <returns></returns>
        private async Task MiningWithInitializingAElfDPoSInformation()
        {
            const ConsensusBehavior behavior = ConsensusBehavior.InitializeAElfDPoS;

            _logger?.Trace($"Trying to enter DPoS Mining Process - {behavior.ToString()}.");

            if (_terminated)
            {
                return;
            }

            if (!CurrentState.AbleToMine())
            {
                return;
            }

            var lockWasTaken = false;
            try
            {
                lockWasTaken = Interlocked.CompareExchange(ref _flag, 1, 0) == 0;
                if (lockWasTaken)
                {
                    MessageHub.Instance.Publish(new DPoSStateChanged(behavior, true));

                    if (MiningLocked())
                    {
                        return;
                    }

                    _logger?.Trace($"Mine - Entered DPoS Mining Process - {behavior.ToString()}.");

                    var logLevel = new Int32Value {Value = LogManager.GlobalThreshold.Ordinal};
                    var parameters = new List<byte[]>
                    {
                        Miners.ToByteArray(),
                        _helper.GenerateInfoForFirstTwoRounds().ToByteArray(),
                        new SInt32Value {Value = ConsensusConfig.Instance.DPoSMiningInterval}.ToByteArray(),
                        logLevel.ToByteArray()
                    };
                    var txToInitializeAElfDPoS = await GenerateTransactionAsync(behavior.ToString(), parameters);
                    await BroadcastTransaction(txToInitializeAElfDPoS);

                    await Mine();
                }
            }
            catch (Exception e)
            {
                _logger?.Trace(e, $"Error in {nameof(MiningWithInitializingAElfDPoSInformation)}");
            }
            finally
            {
                if (lockWasTaken)
                {
                    Thread.VolatileWrite(ref _flag, 0);
                }

                MessageHub.Instance.Publish(new DPoSStateChanged(behavior, false));
                _logger?.Trace(
                    $"Mine - Leaving DPoS Mining Process - {behavior.ToString()}.");
            }
        }

        /// <summary>
        /// Related tx has 5 params:
        /// 1. Current round number
        /// 2. BP Address
        /// 3. Out value
        /// 4. Signature
        /// 5. Round Id
        /// </summary>
        /// <returns></returns>
        private async Task MiningWithPublishingOutValueAndSignature()
        {
            const ConsensusBehavior behavior = ConsensusBehavior.PublishOutValueAndSignature;

            _logger?.Trace($"Trying to enter DPoS Mining Process - {behavior.ToString()}.");

            if (_terminated)
            {
                return;
            }

            if (!CurrentState.AbleToMine())
            {
                return;
            }

            var lockWasTaken = false;
            try
            {
                lockWasTaken = Interlocked.CompareExchange(ref _flag, 1, 0) == 0;
                if (lockWasTaken)
                {
                    MessageHub.Instance.Publish(new DPoSStateChanged(behavior, true));

                    if (MiningLocked())
                    {
                        return;
                    }

                    _logger?.Trace($"Mine - Entered DPoS Mining Process - {behavior.ToString()}.");

                    var inValue = Hash.Generate();
                    if (_consensusData.Count <= 0)
                    {
                        _consensusData.Push(inValue);
                        _consensusData.Push(Hash.FromMessage(inValue));
                    }

                    var currentRoundNumber = _helper.CurrentRoundNumber;
                    var signature = Hash.Default;
                    if (currentRoundNumber.Value > 1)
                    {
                        signature = _helper.CalculateSignature(inValue);
                    }

                    var parameters = new List<byte[]>
                    {
                        _helper.CurrentRoundNumber.ToByteArray(),
                        _consensusData.Pop().ToByteArray(),
                        signature.ToByteArray(),
                        new Int64Value {Value = _helper.GetCurrentRoundInfo().RoundId}.ToByteArray()
                    };

                    var txToPublishOutValueAndSignature =
                        await GenerateTransactionAsync(behavior.ToString(), parameters);
                    await BroadcastTransaction(txToPublishOutValueAndSignature);

                    await Mine();
                }
            }
            catch (Exception e)
            {
                _logger?.Trace(e, $"Error in {nameof(MiningWithPublishingOutValueAndSignature)}");
            }
            finally
            {
                if (lockWasTaken)
                {
                    Thread.VolatileWrite(ref _flag, 0);
                }

                MessageHub.Instance.Publish(new DPoSStateChanged(behavior, false));
                _logger?.Trace($"Mine - Leaving DPoS Mining Process - {behavior.ToString()}.");
            }
        }

        /// <summary>
        /// Related tx has 3 params:
        /// 1. Current round number
        /// 2. BP Address
        /// 3. In value
        /// 4. Round Id
        /// </summary>
        /// <returns></returns>
        private async Task PublishInValue()
        {
            const ConsensusBehavior behavior = ConsensusBehavior.PublishInValue;

            _logger?.Trace($"Trying to enter DPoS Mining Process - {behavior.ToString()}.");

            if (_terminated)
            {
                return;
            }

            if (!CurrentState.AbleToMine())
            {
                return;
            }

            var lockWasTaken = false;
            try
            {
                lockWasTaken = Interlocked.CompareExchange(ref _flag, 1, 0) == 0;
                if (lockWasTaken)
                {
                    MessageHub.Instance.Publish(new DPoSStateChanged(behavior, true));

                    if (MiningLocked())
                    {
                        return;
                    }

                    _logger?.Trace($"Mine - Entered DPoS Mining Process - {behavior.ToString()}.");

                    var currentRoundNumber = _helper.CurrentRoundNumber;

                    if (!_consensusData.Any())
                    {
                        return;
                    }

                    var parameters = new List<byte[]>
                    {
                        currentRoundNumber.ToByteArray(),
                        _consensusData.Pop().ToByteArray(),
                        new Int64Value {Value = _helper.GetCurrentRoundInfo(currentRoundNumber).RoundId}.ToByteArray()
                    };

                    var txToPublishInValue = await GenerateTransactionAsync(behavior.ToString(), parameters);
                    await BroadcastTransaction(txToPublishInValue);
                }
            }
            catch (Exception e)
            {
                _logger?.Trace(e, $"Error in {nameof(PublishInValue)}");
            }
            finally
            {
                if (lockWasTaken)
                {
                    Thread.VolatileWrite(ref _flag, 0);
                }

                MessageHub.Instance.Publish(new DPoSStateChanged(behavior, false));

                _logger?.Trace($"Mine - Leaving DPoS Mining Process - {behavior.ToString()}.");
            }
        }

        /// <summary>
        /// Related tx has 3 params:
        /// 1. Current round info
        /// 2. New round info
        /// 3. Extra block producer of new round
        /// </summary>
        /// <returns></returns>
        private async Task MiningWithUpdatingAElfDPoSInformation()
        {
            const ConsensusBehavior behavior = ConsensusBehavior.UpdateAElfDPoS;

            _logger?.Trace($"Trying to enter DPoS Mining Process - {behavior.ToString()}.");

            if (_terminated)
            {
                return;
            }

            if (!CurrentState.AbleToMine())
            {
                return;
            }

            var lockWasTaken = false;
            try
            {
                lockWasTaken = Interlocked.CompareExchange(ref _flag, 1, 0) == 0;
                if (lockWasTaken)
                {
                    MessageHub.Instance.Publish(new DPoSStateChanged(behavior, true));

                    if (MiningLocked())
                    {
                        return;
                    }

                    _logger?.Trace($"Mine - Entered DPoS Mining Process - {behavior.ToString()}.");

                    var extraBlockResult = _helper.ExecuteTxsForExtraBlock();

                    var parameters = new List<byte[]>
                    {
                        extraBlockResult.Item1.ToByteArray(),
                        extraBlockResult.Item2.ToByteArray(),
                        extraBlockResult.Item3.ToByteArray(),
                        new Int64Value {Value = _helper.GetCurrentRoundInfo().RoundId}.ToByteArray()
                    };

                    var txForExtraBlock = await GenerateTransactionAsync(behavior.ToString(), parameters);

                    await BroadcastTransaction(txForExtraBlock);
                    await Mine();
                }
            }
            catch (Exception e)
            {
                _logger?.Trace(e, $"Error in {nameof(MiningWithUpdatingAElfDPoSInformation)}");
            }
            finally
            {
                if (lockWasTaken)
                {
                    Thread.VolatileWrite(ref _flag, 0);
                }

                MessageHub.Instance.Publish(new DPoSStateChanged(behavior, false));
                _logger?.Trace($"Mine - Leaving DPoS Mining Process - {behavior.ToString()}.");
            }
        }

        public async Task UpdateConsensusEventList()
        {
            _helper.LogDPoSInformation(await BlockChain.GetCurrentBlockHeightAsync());

            if (ConsensusMemory == _helper.CurrentRoundNumber.Value)
                return;

            // Dispose previous observer.
            if (ConsensusDisposable != null)
            {
                ConsensusDisposable.Dispose();
                _logger?.Trace("Disposed previous consensus observables list. Will update DPoS information.");
            }

            // Update observer.
            var miners = _helper.Miners;
            if (!miners.Contains(_nodeAddress))
            {
                return;
            }

            var blockProducerInfoOfCurrentRound = _helper[_nodeAddress];
            ConsensusDisposable = AElfDPoSObserver.SubscribeAElfDPoSMiningProcess(blockProducerInfoOfCurrentRound,
                _helper.ExtraBlockTimeSlot);

            // Update current round number.
            ConsensusMemory = _helper.CurrentRoundNumber.Value;
        }

        public bool IsAlive()
        {
            var currentTime = DateTime.UtcNow;
            var currentRound = _helper.GetCurrentRoundInfo();
            var startTimeSlot = currentRound.BlockProducers.First(bp => bp.Value.Order == 1).Value.TimeSlot
                .ToDateTime();

            var endTimeSlot =
                startTimeSlot.AddMilliseconds(
                    GlobalConfig.BlockProducerNumber * ConsensusConfig.Instance.DPoSMiningInterval * 2);

            return currentTime >
                   startTimeSlot.AddMilliseconds(
                       -GlobalConfig.BlockProducerNumber * ConsensusConfig.Instance.DPoSMiningInterval) ||
                   currentTime < endTimeSlot.AddMilliseconds(ConsensusConfig.Instance.DPoSMiningInterval);
        }

        private async Task BroadcastTransaction(Transaction tx)
        {
            if (tx == null)
            {
                throw new ArgumentException(nameof(tx));
            }

            if (tx.Type == TransactionType.DposTransaction)
            {
                MessageHub.Instance.Publish(new DPoSTransactionGenerated(tx.GetHash().DumpHex()));
                _logger?.Trace(
                    $"A DPoS tx has been generated: {tx.GetHash().DumpHex()} - {tx.MethodName} from {tx.From.GetFormatted()}.");
            }

            if (tx.From.Equals(_nodeAddress))
                _logger?.Trace(
                    $"Try to insert DPoS transaction to pool: {tx.GetHash().DumpHex()} " +
                    $"threadId: {Thread.CurrentThread.ManagedThreadId}");
            
            await _txHub.AddTransactionAsync(tx, true);
        }

        public bool Shutdown()
        {
            _terminated = true;
            return _terminated;
        }

        private static bool MiningLocked()
        {
            return _lockNumber != 0;
        }

        private async Task InitBalance(Address address)
        {
            try
            {
                _logger?.Trace("Entered generating tx.");
                
                var bn = await BlockChain.GetCurrentBlockHeightAsync();
                bn = bn > 4 ? bn - 4 : 0;
                var bh = bn == 0 ? Hash.Genesis : (await BlockChain.GetHeaderByHeightAsync(bn)).GetHash();
                var bhPref = bh.Value.Where((x, i) => i < 4).ToArray();
                
                var tx = new Transaction
                {
                    From = _nodeAddress,
                    To = ContractHelpers.GetTokenContractAddress(Hash.LoadHex(ChainConfig.Instance.ChainId)),
                    RefBlockNumber = bn,
                    RefBlockPrefix = ByteString.CopyFrom(bhPref),
                    MethodName = "InitBalance",
                    Type = TransactionType.ContractTransaction,
                    Params = ByteString.CopyFrom(ParamsPacker.Pack(address, GlobalConfig.LockTokenForElection * 2)),
                };
                
                var signature = new ECSigner().Sign(_nodeKey, tx.GetHash().DumpByteArray());
                
                // Update the signature
                tx.Sigs.Add(ByteString.CopyFrom(signature.SigBytes));

                _logger?.Trace("Leaving generating tx.");

                await BroadcastTransaction(tx);
            }
            catch (Exception e)
            {
                _logger?.Trace(e, "Error while during generating Token tx.");
            }
        }

        private async Task AnnounceElection(Address address)
        {
            try
            {
                _logger?.Trace("Entered generating tx.");
                var bn = await BlockChain.GetCurrentBlockHeightAsync();
                bn = bn > 4 ? bn - 4 : 0;
                var bh = bn == 0 ? Hash.Genesis : (await BlockChain.GetHeaderByHeightAsync(bn)).GetHash();
                var bhPref = bh.Value.Where((x, i) => i < 4).ToArray();
                
                var tx = new Transaction
                {
                    From = address,
                    To = ContractHelpers.GetTokenContractAddress(Hash.LoadHex(ChainConfig.Instance.ChainId)),
                    RefBlockNumber = bn,
                    RefBlockPrefix = ByteString.CopyFrom(bhPref),
                    MethodName = "AnnounceElection",
                    Type = TransactionType.ContractTransaction,
                    Params = ByteString.CopyFrom(ParamsPacker.Pack())
                };
                
                var signature = new ECSigner().Sign(_nodeKey, tx.GetHash().DumpByteArray());
                tx.Sigs.Add(ByteString.CopyFrom(signature.SigBytes));

                _logger?.Trace("Leaving generating tx.");

                await BroadcastTransaction(tx);
            }
            catch (Exception e)
            {
                _logger?.Trace(e, "Error while during generating Token tx.");
            }
        }

        private async Task GenerateVoteTransactionAsync(Address voter, Address candidate)
        {
            try
            {
                _logger?.Trace("Entered generating tx.");
                
                var bn = await BlockChain.GetCurrentBlockHeightAsync();
                bn = bn > 4 ? bn - 4 : 0;
                var bh = bn == 0 ? Hash.Genesis : (await BlockChain.GetHeaderByHeightAsync(bn)).GetHash();
                var bhPref = bh.Value.Where((x, i) => i < 4).ToArray();
                
                var tx = new Transaction
                {
                    From = voter,
                    To = ContractHelpers.GetConsensusContractAddress(Hash.LoadHex(ChainConfig.Instance.ChainId)),
                    RefBlockNumber = bn,
                    RefBlockPrefix = ByteString.CopyFrom(bhPref),
                    MethodName = "AnnounceElection",
                    Type = TransactionType.ContractTransaction,
                    Params = ByteString.CopyFrom(ParamsPacker.Pack(candidate.ToByteArray(),
                        new UInt64Value {Value = (ulong) new Random().Next(1, 10)}.ToByteArray()))
                };
                
                var signature = new ECSigner().Sign(_nodeKey, tx.GetHash().DumpByteArray());
                tx.Sigs.Add(ByteString.CopyFrom(signature.SigBytes));

                _logger?.Trace("Leaving generating tx.");

                await BroadcastTransaction(tx);
            }
            catch (Exception e)
            {
                _logger?.Trace(e, "Error while during generating Token tx.");
            }
        }
    }
}