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
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Kernel.EventMessages;
using AElf.Kernel.Managers;
using AElf.Miner.Miner;
using AElf.Types.CSharp;
using Easy.MessageHub;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using AElf.Miner.TxMemPool;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

// ReSharper disable once CheckNamespace
namespace AElf.Node.Consensus
{
    // ReSharper disable InconsistentNaming
    public class DPoS : IConsensus
    {
        private ulong LatestRoundNumber { get; set; }

        private ulong LatestTermNumber { get; set; }

        private static IDisposable ConsensusDisposable { get; set; }

        private bool _consensusInitialized;

        private bool _minerFlag;

        private readonly ITxHub _txHub;
        private readonly IMiner _miner;
        private readonly IChainService _chainService;

        private IBlockChain _blockChain;

        private IBlockChain BlockChain => _blockChain ?? (_blockChain =
                                              _chainService.GetBlockChain(
                                                  Hash.LoadByteArray(ChainConfig.Instance.ChainId.DecodeBase58())));

        public ILogger<DPoS> Logger {get;set;}

        private readonly ConsensusHelper _helper;

        private static int _lockNumber;

        private NodeState CurrentState { get; set; } = NodeState.Catching;

        /// <summary>
        /// In Value and Out Value.
        /// </summary>
        private readonly Stack<Hash> _consensusData = new Stack<Hash>();

        private ECKeyPair _nodeKey;
        private byte[] _ownPubKey;

        private readonly Hash _chainId;

        public Address ConsensusContractAddress =>
            ContractHelpers.GetConsensusContractAddress(Hash.LoadBase58(ChainConfig.Instance.ChainId));

        public Address TokenContractAddress =>
            ContractHelpers.GetTokenContractAddress(Hash.LoadBase58(ChainConfig.Instance.ChainId));
        
        private readonly IMinersManager _minersManager;

        private static int _flag;


        private static bool _executedBlockFromOtherMiners;

        private ConsensusObserver ConsensusObserver =>
            new ConsensusObserver(InitialTerm, PackageOutValue, BroadcastInValue, NextRound, NextTerm);

        public DPoS(ITxHub txHub, IMiner miner, IChainService chainService, IMinersManager minersManager,
            ConsensusHelper helper)
        {
            _txHub = txHub;
            _miner = miner;
            _chainService = chainService;
            _minersManager = minersManager;
            _helper = helper;

            _chainId = Hash.LoadByteArray(ChainConfig.Instance.ChainId.DecodeBase58());

            Logger= NullLogger<DPoS>.Instance;
            var count = MinersConfig.Instance.Producers.Count;

            GlobalConfig.BlockProducerNumber = count;
            GlobalConfig.BlockNumberOfEachRound = count + 1;

            Logger.LogInformation("Block Producer nodes count:" + GlobalConfig.BlockProducerNumber);
            Logger.LogInformation("Blocks of one round:" + GlobalConfig.BlockNumberOfEachRound);
            MessageHub.Instance.Subscribe<UpdateConsensus>(async option =>
            {
                if (option == UpdateConsensus.UpdateAfterExecution)
                {
                    _executedBlockFromOtherMiners = true;
                    Logger.LogTrace("UpdateConsensus - Update");
                    await UpdateConsensusInformation();
                }

                if (option == UpdateConsensus.UpdateAfterMining)
                {
                    Logger.LogTrace("UpdateConsensus - Update");
                    await UpdateConsensusInformation();
                }

                if (option == UpdateConsensus.Dispose)
                {
                    Logger.LogTrace("UpdateConsensus - Dispose");
                    DisposeConsensusEventList();
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

            MessageHub.Instance.Subscribe<FSMStateChanged>(inState => { CurrentState = inState.CurrentState; });
        }

        private Miners Miners => _minersManager.GetMiners().Result;

        public void Start(bool willToMine)
        {
            _nodeKey = NodeConfig.Instance.ECKeyPair;
            _ownPubKey = _nodeKey.PublicKey;

            if (!willToMine)
            {
                return;
            }

            // Consensus information already generated.
            if (ConsensusDisposable != null)
            {
                return;
            }

            if (_consensusInitialized)
                return;

            _consensusInitialized = true;

            // Check whether this node contained BP list.
            if (!Miners.PublicKeys.Contains(_ownPubKey.ToHex()))
            {
                return;
            }

            if (!_minersManager.IsMinersInDatabase().Result)
            {
                ConsensusDisposable = ConsensusObserver.Initialization();
                return;
            }

            _helper.SyncMiningInterval();

            if (_helper.CanRecoverDPoSInformation())
            {
                ConsensusDisposable = ConsensusObserver.RecoverMining();
            }
        }

        public void DisposeConsensusEventList()
        {
            ConsensusDisposable?.Dispose();
            ConsensusDisposable = null;
            Logger.LogTrace("Mining stopped. Disposed previous consensus observables list.");
        }

        public void IncrementLockNumber()
        {
            Interlocked.Add(ref _lockNumber, 1);
            Logger.LogTrace($"Lock number increment: {_lockNumber}");
        }

        public void DecrementLockNumber()
        {
            if (_lockNumber <= 0)
            {
                return;
            }

            Interlocked.Add(ref _lockNumber, -1);
            Logger.LogTrace($"Lock number decrement: {_lockNumber}");
        }

        private async Task<IBlock> Mine()
        {
            try
            {
                var block = await _miner.Mine();

                return block;
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Exception while mining.");
                return null;
            }
        }

        private async Task<Transaction> GenerateDPoSTransactionAsync(string methodName, List<object> parameters)
        {
            try
            {
                Logger.LogTrace("Entered generating tx.");
                var bn = await BlockChain.GetCurrentBlockHeightAsync();
                bn = bn > 4 ? bn - 4 : 0;
                var bh = bn == 0 ? Hash.Genesis : (await BlockChain.GetHeaderByHeightAsync(bn)).GetHash();
                var bhPref = bh.Value.Where((x, i) => i < 4).ToArray();

                var tx = new Transaction
                {
                    From = Address.FromPublicKey(_ownPubKey),
                    To = ConsensusContractAddress,
                    RefBlockNumber = bn,
                    RefBlockPrefix = ByteString.CopyFrom(bhPref),
                    MethodName = methodName,
                    Type = TransactionType.DposTransaction,
                    Params = ByteString.CopyFrom(ParamsPacker.Pack(parameters.ToArray()))
                };

                var signer = new ECSigner();
                var signature = signer.Sign(_nodeKey, tx.GetHash().DumpByteArray());
                tx.Sigs.Add(ByteString.CopyFrom(signature.SigBytes));

                Logger.LogTrace("Leaving generating tx.");

                MessageHub.Instance.Publish(StateEvent.ConsensusTxGenerated);

                return tx;
            }
            catch (Exception e)
            {
                Logger.LogTrace(e, "Error while during generating DPoS tx.");
            }

            return null;
        }
        
        private async Task<Transaction> GenerateNormalTransactionAsync(string methodName, List<object> parameters)
        {
            try
            {
                var bn = await BlockChain.GetCurrentBlockHeightAsync();
                bn = bn > 4 ? bn - 4 : 0;
                var bh = bn == 0 ? Hash.Genesis : (await BlockChain.GetHeaderByHeightAsync(bn)).GetHash();
                var bhPref = bh.Value.Where((x, i) => i < 4).ToArray();

                var tx = new Transaction
                {
                    From = Address.FromPublicKey(_ownPubKey),
                    To = TokenContractAddress,
                    RefBlockNumber = bn,
                    RefBlockPrefix = ByteString.CopyFrom(bhPref),
                    MethodName = methodName,
                    Type = TransactionType.ContractTransaction,
                    Params = ByteString.CopyFrom(ParamsPacker.Pack(parameters.ToArray()))
                };

                var signer = new ECSigner();
                var signature = signer.Sign(_nodeKey, tx.GetHash().DumpByteArray());
                tx.Sigs.Add(ByteString.CopyFrom(signature.SigBytes));

                return tx;
            }
            catch (Exception e)
            {
                Logger.LogTrace(e, "Error while during generating normal tx.");
            }

            return null;
        }

        private async Task InitialTerm()
        {
            const ConsensusBehavior behavior = ConsensusBehavior.InitialTerm;

            Logger.LogTrace($"Trying to enter DPoS Mining Process - {behavior.ToString()}.");

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

                    Logger.LogTrace($"Mine - Entered DPoS Mining Process - {behavior.ToString()}.");

                    var firstTerm = _minersManager.GetMiners().Result
                        .GenerateNewTerm(ConsensusConfig.Instance.DPoSMiningInterval);
                    
                    //TODO! should not pass any parameters about logging system
                    //var logLevel = new Int32Value {Value = LogManager.GlobalThreshold.Ordinal};
var logLevel = new Int32Value {Value = 0};
                    
                    var parameters = new List<object>
                    {
                        firstTerm,
                        logLevel
                    };
                    var txToInitialTerm = await GenerateDPoSTransactionAsync(behavior.ToString(), parameters);
                    await BroadcastTransaction(txToInitialTerm);

                    await Mine();
                }
            }
            catch (Exception e)
            {
                Logger.LogTrace(e, $"Error in {nameof(InitialTerm)}");
            }
            finally
            {
                if (lockWasTaken)
                {
                    Thread.VolatileWrite(ref _flag, 0);
                }

                MessageHub.Instance.Publish(new DPoSStateChanged(behavior, false));
                Logger.LogTrace($"Mine - Leaving DPoS Mining Process - {behavior.ToString()}.");
            }
        }

        private async Task PackageOutValue()
        {
            const ConsensusBehavior behavior = ConsensusBehavior.PackageOutValue;

            Logger.LogTrace($"Trying to enter DPoS Mining Process - {behavior.ToString()}.");

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

                    Logger.LogTrace($"Mine - Entered DPoS Mining Process - {behavior.ToString()}.");

                    var inValue = Hash.Generate();
                    if (_consensusData.Count <= 0)
                    {
                        _consensusData.Push(inValue);
                        _consensusData.Push(Hash.FromMessage(inValue));
                    }

                    var currentRoundNumber = _helper.CurrentRoundNumber;
                    var roundInfo = _helper.GetCurrentRoundInfo();

                    var signature = Hash.Default;

                    if (_helper.TryGetRoundInfo(currentRoundNumber.Value - 1, out var previousRoundInfo))
                    {
                        signature = previousRoundInfo.CalculateSignature(inValue);
                    }

                    var parameters = new List<object>
                    {
                        new ToPackage
                        {
                            OutValue = _consensusData.Pop(),
                            Signature = signature,
                            RoundId = roundInfo.RoundId
                        }
                    };

                    var txToPackageOutValue =
                        await GenerateDPoSTransactionAsync(behavior.ToString(), parameters);
                    await BroadcastTransaction(txToPackageOutValue);

                    await Mine();
                }
            }
            catch (Exception e)
            {
                Logger.LogTrace(e, $"Error in {nameof(PackageOutValue)}");
            }
            finally
            {
                if (lockWasTaken)
                {
                    Thread.VolatileWrite(ref _flag, 0);
                }

                MessageHub.Instance.Publish(new DPoSStateChanged(behavior, false));
                Logger.LogTrace($"Mine - Leaving DPoS Mining Process - {behavior.ToString()}.");

                await BroadcastInValue();
            }
        }

        private async Task BroadcastInValue()
        {
            const ConsensusBehavior behavior = ConsensusBehavior.BroadcastInValue;

            Logger.LogTrace($"Trying to enter DPoS Mining Process - {behavior.ToString()}.");

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

                    Logger.LogTrace($"Mine - Entered DPoS Mining Process - {behavior.ToString()}.");

                    var roundInfo = _helper.GetCurrentRoundInfo();

                    if (!_consensusData.Any())
                    {
                        return;
                    }

                    var parameters = new List<object>
                    {
                        new ToBroadcast
                        {
                            InValue = _consensusData.Pop(),
                            RoundId = roundInfo.RoundId
                        }
                    };

                    var txToPublishInValue = await GenerateDPoSTransactionAsync(behavior.ToString(), parameters);
                    await BroadcastTransaction(txToPublishInValue);
                }
            }
            catch (Exception e)
            {
                Logger.LogTrace(e, $"Error in {nameof(BroadcastInValue)}");
            }
            finally
            {
                if (lockWasTaken)
                {
                    Thread.VolatileWrite(ref _flag, 0);
                }

                MessageHub.Instance.Publish(new DPoSStateChanged(behavior, false));
                Logger.LogTrace($"Mine - Leaving DPoS Mining Process - {behavior.ToString()}.");
            }
        }

        private async Task NextRound()
        {
            const ConsensusBehavior behavior = ConsensusBehavior.NextRound;
            var goNextTerm = false;

            Logger.LogTrace($"Trying to enter DPoS Mining Process - {behavior.ToString()}.");

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

                    Logger.LogTrace($"Mine - Entered DPoS Mining Process - {behavior.ToString()}.");

                    var currentRoundNumber = _helper.CurrentRoundNumber;
                    var roundInfo = _helper.GetCurrentRoundInfo();
                    if (_helper.TryGetRoundInfo(currentRoundNumber.Value - 1, out var previousRoundInfo))
                    {
                        roundInfo = roundInfo.Supplement(previousRoundInfo);
                    }
                    else
                    {
                        roundInfo = roundInfo.SupplementForFirstRound();
                    }

                    var nextRoundInfo = _minersManager.GetMiners().Result.GenerateNextRound(roundInfo.Clone());

                    var calculatedAge = _helper.CalculateBlockchainAge();
                    Logger.LogTrace("Current blockchain age: " + calculatedAge);

                    // TODO: Recover after testing
                    if (/*(calculatedAge % GlobalConfig.DaysEachTerm == 0 &&
                         calculatedAge / GlobalConfig.DaysEachTerm <= LatestTermNumber) ||*/
                        (LatestRoundNumber / GlobalConfig.RoundsPerTerm + 1 != LatestTermNumber &&
                         _helper.TryToGetVictories(out var victories) &&
                         victories.Count == GlobalConfig.BlockProducerNumber))
                    {
                        Logger.LogTrace("Will change term.");
                        throw new NextTermException();
                    }

                    var miners = Miners;

                    foreach (var minerInRound in nextRoundInfo.RealTimeMinersInfo.Values)
                    {
                        if (minerInRound.MissedTimeSlots >= GlobalConfig.MaxMissedTimeSlots)
                        {
                            var poorGuyPublicKey = minerInRound.PublicKey;
                            var latestTermSnapshot = _helper.GetLatestTermSnapshot();
                            var luckyGuyPublicKey = latestTermSnapshot.GetNextCandidate(miners);

                            nextRoundInfo.RealTimeMinersInfo[luckyGuyPublicKey] =
                                nextRoundInfo.RealTimeMinersInfo[poorGuyPublicKey];
                            nextRoundInfo.RealTimeMinersInfo[luckyGuyPublicKey].MissedTimeSlots = 0;
                            nextRoundInfo.RealTimeMinersInfo[luckyGuyPublicKey].ProducedBlocks = 0;
                            nextRoundInfo.RealTimeMinersInfo.Remove(poorGuyPublicKey);

                            miners.PublicKeys.Remove(poorGuyPublicKey);
                            miners.PublicKeys.Add(luckyGuyPublicKey);
                        }
                    }

                    await _minersManager.SetMiners(miners);

                    var parameters = new List<object>
                    {
                        new Forwarding
                        {
                            CurrentRoundInfo = roundInfo,
                            NextRoundInfo = nextRoundInfo,
                            CurrentAge = calculatedAge
                        }
                    };

                    var txForNextRound = await GenerateDPoSTransactionAsync(behavior.ToString(), parameters);

                    await BroadcastTransaction(txForNextRound);
                    await Mine();
                }
            }
            catch (NextTermException)
            {
                goNextTerm = true;
            }
            catch (Exception e)
            {
                Logger.LogTrace(e, $"Error in {nameof(NextRound)}");
            }
            finally
            {
                if (lockWasTaken)
                {
                    Thread.VolatileWrite(ref _flag, 0);
                }

                MessageHub.Instance.Publish(new DPoSStateChanged(behavior, false));
                Logger.LogTrace($"Mine - Leaving DPoS Mining Process - {behavior.ToString()}.");

                if (goNextTerm)
                {
                    ConsensusDisposable?.Dispose();
                    ConsensusDisposable = ConsensusObserver.NextTerm();
                }
            }
        }

        private async Task NextTerm()
        {
            const ConsensusBehavior behavior = ConsensusBehavior.NextTerm;

            Logger.LogTrace($"Trying to enter DPoS Mining Process - {behavior.ToString()}.");

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

                    Logger.LogTrace($"Mine - Entered DPoS Mining Process - {behavior.ToString()}.");

                    _helper.TryToGetVictories(out var victories);
                    var parameters = new List<object>
                    {
                        victories.ToMiners().GenerateNewTerm(ConsensusConfig.Instance.DPoSMiningInterval,
                            _helper.CurrentRoundNumber.Value + 1, _helper.CurrentTermNumber.Value)
                    };

                    var txForNextTerm = await GenerateDPoSTransactionAsync(behavior.ToString(), parameters);

                    await BroadcastTransaction(txForNextTerm);
                    await Mine();
                }
            }
            catch (Exception e)
            {
                Logger.LogTrace(e, $"Error in {nameof(NextRound)}");
            }
            finally
            {
                if (lockWasTaken)
                {
                    Thread.VolatileWrite(ref _flag, 0);
                }

                MessageHub.Instance.Publish(new DPoSStateChanged(behavior, false));
                Logger.LogTrace($"Mine - Leaving DPoS Mining Process - {behavior.ToString()}.");
            }
        }

        public async Task UpdateConsensusInformation()
        {
            _helper.LogDPoSInformation(await BlockChain.GetCurrentBlockHeightAsync());

            if (LatestRoundNumber == _helper.CurrentRoundNumber.Value)
            {
                return;
            }

            // Update miners.
            
            if (_helper.TryGetRoundInfo(LatestRoundNumber, out var previousRoundInfo))
            {
                var currentRoundInfo = _helper.GetCurrentRoundInfo();
                if (currentRoundInfo.MinersHash() != previousRoundInfo.MinersHash())
                {
                    Logger.LogTrace("Updating miners.");
                    await _minersManager.SetMiners(_helper.GetCurrentMiners());
                }
            }

            if (_executedBlockFromOtherMiners && _helper.GetCurrentRoundInfo().CheckWhetherMostMinersMissedTimeSlots())
            {
                MessageHub.Instance.Publish(new MinorityForkDetected());
            }
            
            // Update current round number and current term number.
            LatestRoundNumber = _helper.CurrentRoundNumber.Value;
            LatestTermNumber = _helper.CurrentTermNumber.Value;

            if (!NodeConfig.Instance.IsMiner)
            {
                return;
            }

            // Dispose previous observer.
            if (ConsensusDisposable != null)
            {
                ConsensusDisposable.Dispose();
                ConsensusDisposable = null;
                Logger.LogTrace("Disposed previous consensus observables list. Will update DPoS information.");
            }

            // Update observer.
            var miners = _helper.Miners;
            if (miners.All(m => m != _ownPubKey.ToHex()))
            {
                _minerFlag = false;
                return;
            }

            if (!_minerFlag)
            {
                Logger.LogTrace("Become a miner just now.");
            }

            _minerFlag = true;

            ConsensusDisposable = ConsensusObserver.SubscribeMiningProcess(_helper.GetCurrentRoundInfo());
        }

        public bool IsAlive()
        {
            var currentTime = DateTime.UtcNow;
            var currentRound = _helper.GetCurrentRoundInfo();
            var startTimeSlot = currentRound.RealTimeMinersInfo.First(bp => bp.Value.Order == 1).Value
                .ExpectedMiningTime
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
                MessageHub.Instance.Publish(new DPoSTransactionGenerated(tx.GetHash().ToHex()));
                Logger.LogTrace(
                    $"A DPoS tx has been generated: {tx.GetHash().ToHex()} - {tx.MethodName} from {tx.From.GetFormatted()}.");
            }

            if (tx.From.Equals(_ownPubKey))
                Logger.LogTrace(
                    $"Try to insert DPoS transaction to pool: {tx.GetHash().ToHex()} " +
                    $"threadId: {Thread.CurrentThread.ManagedThreadId}");

            await _txHub.AddTransactionAsync(tx, true);
        }

        public bool Shutdown()
        {
            return true;
        }

        private static bool MiningLocked()
        {
            return _lockNumber != 0;
        }
    }
}