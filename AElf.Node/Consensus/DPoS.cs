using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.ChainController.EventMessages;
using AElf.Common;
using AElf.Common.FSM;
using AElf.Configuration;
using AElf.Configuration.Config.Consensus;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Account;
using AElf.Kernel.Consensus;
using AElf.Kernel.EventMessages;
using AElf.Kernel.Extensions;
using AElf.Kernel.Managers;
using AElf.Kernel.Miner;
using AElf.Kernel.TxMemPool;
using AElf.Kernel.Types;
using AElf.Types.CSharp;
using Easy.MessageHub;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
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
        private readonly IMinerService _minerService;
        private readonly IChainService _chainService;
        private readonly IAccountService _accountService;

        private IBlockChain _blockChain;

        private IBlockChain BlockChain => _blockChain ?? (_blockChain =
                                              _chainService.GetBlockChain(
                                                  _chainId));

        public ILogger<DPoS> Logger {get;set;}

        private readonly ConsensusHelper _helper;

        private NodeState CurrentState { get; set; } = NodeState.Catching;

        /// <summary>
        /// In Value and Out Value.
        /// </summary>
        private readonly Stack<Hash> _consensusData = new Stack<Hash>();

        private byte[] _ownPubKey;

        private Address ConsensusContractAddress =>
            ContractHelpers.GetConsensusContractAddress(_chainId);

        private readonly IMinersManager _minersManager;

        private static int _lockNumber;

        private static int _lockFlag;


        private static bool _executedBlockFromOtherMiners;

        private static bool _amIMined;

        private static bool _announcedElection;

        private static ulong _firstTermChangedRoundNumber;

        private string _publicKey;

        // TODO: Shouldn't keep it in here, remove it after module refactor
        private int _chainId;

        private ConsensusObserver ConsensusObserver =>
            new ConsensusObserver(_publicKey, InitialTerm, PackageOutValue, BroadcastInValue, NextRound, NextTerm);

        public DPoS(ITxHub txHub, IChainService chainService, IMinersManager minersManager,
            ConsensusHelper helper,IAccountService accountService, IMinerService minerService)
        {
            _txHub = txHub;
            _chainService = chainService;
            _minersManager = minersManager;
            _helper = helper;
            _accountService = accountService;
            _minerService = minerService;

            Logger = NullLogger<DPoS>.Instance;

            _publicKey = _accountService.GetPublicKeyAsync().Result.ToHex();
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
                    _amIMined = true;
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

            MessageHub.Instance.Subscribe<NewLibFound>(libState =>
            {
                // TODO: Should get the round number LIB height.
                if (_chainId.DumpBase58() == GlobalConfig.DefaultChainId && 
                    _helper.TryGetRoundInfo(_chainId,_helper.GetCurrentRoundNumber(_chainId).Value, out var round))
                {
                    var miners = round.RealTimeMinersInfo.Keys.ToMiners();
                    miners.TermNumber = LatestTermNumber;
                    _minersManager.SetMiners(miners, _chainId);
                }
            });
        }

        private Miners Miners
        {
            get
            {
                if (_chainId.DumpBase58() == GlobalConfig.DefaultChainId)
                {
                    if (_helper.GetCurrentTermNumber(_chainId).Value == 0)
                    {
                        return _minersManager.GetMiners(0).Result;
                    }

                    return _helper.GetCurrentMiners(_chainId);
                }

                var roundInfo = _helper.GetCurrentRoundInfo(_chainId);
                if (roundInfo != null)
                {
                    var miners = _minersManager.GetMiners(roundInfo.MinersTermNumber).Result;
                    Logger.LogTrace($"Sidechain getting miners: {miners.PublicKeys}");
                    return miners;
                }

                var basicMiners = _minersManager.GetMiners(1).Result;
                var mainchainLatestTermNumber = basicMiners.MainchainLatestTermNumber;
                if (mainchainLatestTermNumber != 0)
                {
                    return _minersManager.GetMiners(mainchainLatestTermNumber).Result;
                }

                return _minersManager.GetMiners(1).Result;
            }
        }

        public void Start(bool willingToMine, int chainId)
        {
            _chainId = chainId;
            _ownPubKey = _accountService.GetPublicKeyAsync().Result;

            if (!willingToMine)
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

            if (!_minersManager.IsMinersInDatabase().Result ||
                _chainId.DumpBase58() != GlobalConfig.DefaultChainId)
            {
                ConsensusDisposable = ConsensusObserver.Initialization();
                return;
            }

            _helper.SyncMiningInterval(_chainId);

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

        private void IncrementLockNumber()
        {
            Interlocked.Add(ref _lockNumber, 1);
            Logger.LogTrace($"Lock number increment: {_lockNumber}");
        }

        private void DecrementLockNumber()
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
                var block = await _minerService.Mine(_chainId);

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

                var signature = await _accountService.SignAsync(tx.GetHash().DumpByteArray());
                tx.Sigs.Add(ByteString.CopyFrom(signature));

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
                lockWasTaken = Interlocked.CompareExchange(ref _lockFlag, 1, 0) == 0;
                if (lockWasTaken)
                {
                    MessageHub.Instance.Publish(new DPoSStateChanged(behavior, true));

                    if (MiningLocked())
                    {
                        return;
                    }

                    Logger.LogTrace($"Mine - Entered DPoS Mining Process - {behavior.ToString()}.");

                    Term firstTerm;

                    var initialMiners = await _minersManager.GetMiners(0);
                    var basicMiners = await _minersManager.GetMiners(1);

                    if (_chainId.DumpBase58() != GlobalConfig.DefaultChainId && basicMiners != null &&
                        basicMiners.MainchainLatestTermNumber != 0)
                    {
                        var minersTermNumber = basicMiners.MainchainLatestTermNumber;
                        firstTerm = (await _minersManager.GetMiners(minersTermNumber)).GenerateNewTerm(ConsensusConfig
                            .Instance
                            .DPoSMiningInterval);
                        firstTerm.FirstRound.MinersTermNumber = minersTermNumber;
                        firstTerm.SecondRound.MinersTermNumber = minersTermNumber;
                    }
                    else
                    {
                        await _minersManager.SetMiners(initialMiners, _chainId);
                        firstTerm = initialMiners.GenerateNewTerm(ConsensusConfig.Instance.DPoSMiningInterval);
                    }

                    Logger.LogTrace($"Initial consensus information: {firstTerm}");
                    
                    
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
                    Thread.VolatileWrite(ref _lockFlag, 0);
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
                lockWasTaken = Interlocked.CompareExchange(ref _lockFlag, 1, 0) == 0;
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

                    var currentRoundNumber = _helper.GetCurrentRoundNumber(_chainId);
                    var roundInfo = _helper.GetCurrentRoundInfo(_chainId);

                    var signature = Hash.Default;

                    if (_helper.TryGetRoundInfo(_chainId, currentRoundNumber.Value - 1, out var previousRoundInfo))
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
                    Thread.VolatileWrite(ref _lockFlag, 0);
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
                lockWasTaken = Interlocked.CompareExchange(ref _lockFlag, 1, 0) == 0;
                if (lockWasTaken)
                {
                    MessageHub.Instance.Publish(new DPoSStateChanged(behavior, true));

                    if (MiningLocked())
                    {
                        return;
                    }

                    Logger.LogTrace($"Mine - Entered DPoS Mining Process - {behavior.ToString()}.");

                    var roundInfo = _helper.GetCurrentRoundInfo(_chainId);

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
                    Thread.VolatileWrite(ref _lockFlag, 0);
                }

                MessageHub.Instance.Publish(new DPoSStateChanged(behavior, false));
                Logger.LogTrace($"Mine - Leaving DPoS Mining Process - {behavior.ToString()}.");
            }
        }

        private async Task NextRound()
        {
            const ConsensusBehavior behavior = ConsensusBehavior.NextRound;

            Logger.LogTrace($"Trying to enter DPoS Mining Process - {behavior.ToString()}.");

            if (!CurrentState.AbleToMine())
            {
                return;
            }

            var lockWasTaken = false;
            try
            {
                lockWasTaken = Interlocked.CompareExchange(ref _lockFlag, 1, 0) == 0;
                if (lockWasTaken)
                {
                    MessageHub.Instance.Publish(new DPoSStateChanged(behavior, true));

                    if (MiningLocked())
                    {
                        return;
                    }

                    Logger.LogTrace($"Mine - Entered DPoS Mining Process - {behavior.ToString()}.");

                    var currentRoundNumber = _helper.GetCurrentRoundNumber(_chainId);
                    Logger.LogTrace("Round number: " + currentRoundNumber);
                    
                    var roundInfo = _helper.GetCurrentRoundInfo(_chainId);
                    roundInfo = _helper.TryGetRoundInfo(_chainId, currentRoundNumber.Value - 1, out var previousRoundInfo)
                        ? roundInfo.Supplement(previousRoundInfo)
                        : roundInfo.SupplementForFirstRound();

                    var miners = Miners;

                    var nextRoundInfo = miners.GenerateNextRound(_chainId, roundInfo.Clone());

                    var calculatedAge = _helper.CalculateBlockchainAge(_chainId);
                    Logger.LogTrace("Current blockchain age: " + calculatedAge);

                    if (CanStartNextTerm())
                    {
                        Thread.VolatileWrite(ref _lockFlag, 0);

                        MessageHub.Instance.Publish(new DPoSStateChanged(behavior, false));

                        Logger.LogTrace($"Mine - Leaving DPoS Mining Process - {behavior.ToString()}.");
                        Logger.LogTrace("Will change term.");

                        ConsensusDisposable?.Dispose();
                        ConsensusDisposable = ConsensusObserver.NextTerm();

                        return;
                    }

                    foreach (var minerInRound in nextRoundInfo.RealTimeMinersInfo.Values)
                    {
                        if (minerInRound.MissedTimeSlots < GlobalConfig.MaxMissedTimeSlots)
                            continue;

                        var poorGuyPublicKey = minerInRound.PublicKey;
                        var latestTermSnapshot = _helper.GetLatestTermSnapshot(_chainId);
                        var luckyGuyPublicKey = latestTermSnapshot.GetNextCandidate(miners);

                        nextRoundInfo.RealTimeMinersInfo[luckyGuyPublicKey] =
                            nextRoundInfo.RealTimeMinersInfo[poorGuyPublicKey];
                        nextRoundInfo.RealTimeMinersInfo[luckyGuyPublicKey].MissedTimeSlots = 0;
                        nextRoundInfo.RealTimeMinersInfo[luckyGuyPublicKey].ProducedBlocks = 0;
                        nextRoundInfo.RealTimeMinersInfo.Remove(poorGuyPublicKey);

                        miners.PublicKeys.Remove(poorGuyPublicKey);
                        miners.PublicKeys.Add(luckyGuyPublicKey);
                    }

                    if (_chainId.DumpBase58() == GlobalConfig.DefaultChainId)
                    {
                        await _minersManager.SetMiners(miners, _chainId);
                    }
                    else
                    {
                        var minersTermNumber = (await _minersManager.GetMiners(1)).MainchainLatestTermNumber;
                        nextRoundInfo.MinersTermNumber = minersTermNumber;
                        Logger.LogTrace("Sidechain set miners term number to: " + minersTermNumber);
                    }
                    
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
            catch (Exception e)
            {
                Logger.LogTrace(e, $"Error in {nameof(NextRound)}");
            }
            finally
            {
                if (lockWasTaken)
                {
                    Thread.VolatileWrite(ref _lockFlag, 0);
                }

                MessageHub.Instance.Publish(new DPoSStateChanged(behavior, false));
                Logger.LogTrace($"Mine - Leaving DPoS Mining Process - {behavior.ToString()}.");
            }
        }

        private async Task NextTerm()
        {
            if (_chainId.DumpBase58() != GlobalConfig.DefaultChainId)
            {
                Logger.LogWarning("Unexpected entering of next term becuase current chian is side chain.");
                return;
            }

            const ConsensusBehavior behavior = ConsensusBehavior.NextTerm;

            Logger.LogTrace($"Trying to enter DPoS Mining Process - {behavior.ToString()}.");

            if (!CurrentState.AbleToMine())
            {
                return;
            }

            var lockWasTaken = false;
            try
            {
                lockWasTaken = Interlocked.CompareExchange(ref _lockFlag, 1, 0) == 0;
                if (lockWasTaken)
                {
                    MessageHub.Instance.Publish(new DPoSStateChanged(behavior, true));

                    if (MiningLocked())
                    {
                        return;
                    }

                    Logger.LogTrace($"Mine - Entered DPoS Mining Process - {behavior.ToString()}.");

                    Term nextTerm;
                    if (_helper.TryToGetVictories(_chainId, out var victories))
                    {
                        nextTerm = victories.ToMiners().GenerateNewTerm(ConsensusConfig.Instance.DPoSMiningInterval,
                            _helper.GetCurrentRoundNumber(_chainId).Value, _helper.GetCurrentTermNumber(_chainId).Value);
                    }
                    else
                    {
                        nextTerm = (await _minersManager.GetMiners(0)).GenerateNewTerm(
                            ConsensusConfig.Instance.DPoSMiningInterval, _helper.GetCurrentRoundNumber(_chainId).Value,
                            _helper.GetCurrentTermNumber(_chainId).Value);
                    }

                    var txs = new List<Transaction>
                    {
                        await GenerateDPoSTransactionAsync(behavior.ToString(), new List<object> {nextTerm}),
                        await GenerateDPoSTransactionAsync(
                            ConsensusBehavior.SnapshotForTerm.ToString(),
                            new List<object> {_helper.GetCurrentTermNumber(_chainId).Value, _helper.GetCurrentRoundNumber(_chainId).Value}),
                        await GenerateDPoSTransactionAsync(
                            ConsensusBehavior.SnapshotForMiners.ToString(),
                            new List<object> {_helper.GetCurrentTermNumber(_chainId).Value, _helper.GetCurrentRoundNumber(_chainId).Value}),
                        await GenerateDPoSTransactionAsync(
                            ConsensusBehavior.SendDividends.ToString(),
                            new List<object> {_helper.GetCurrentTermNumber(_chainId).Value, _helper.GetCurrentRoundNumber(_chainId).Value}),
                    };

                    foreach (var transaction in txs)
                    {
                        await BroadcastTransaction(transaction);
                    }

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
                    Thread.VolatileWrite(ref _lockFlag, 0);
                }

                MessageHub.Instance.Publish(new DPoSStateChanged(behavior, false));
                Logger.LogTrace($"Mine - Leaving DPoS Mining Process - {behavior.ToString()}.");
            }
        }

        public async Task UpdateConsensusInformation()
        {
            _helper.LogDPoSInformation(_chainId, await BlockChain.GetCurrentBlockHeightAsync());

            if (_chainId.DumpBase58() == GlobalConfig.DefaultChainId)
            {
                if (AmIContainedInCandidatesList())
                {
                    // Not record as announced before.
                    if (!_announcedElection)
                    {
                        Logger.LogTrace("This node announced election.");
                        _announcedElection = true;
                    }
                }
                else
                {
                    // Record as announced before.
                    if (_announcedElection)
                    {
                        Logger.LogTrace("This node quit election.");
                        _announcedElection = false;
                    }
                }
            }

            // To detect whether the round number has changed.
            // When the round number changed, it means this node has to update his consensus events, 
            // or update the miners list.
            if (LatestRoundNumber == _helper.GetCurrentRoundNumber(_chainId).Value)
            {
                return;
            }

            if (_executedBlockFromOtherMiners && _amIMined &&
                _helper.GetCurrentRoundInfo(_chainId).CheckWhetherMostMinersMissedTimeSlots())
            {
                MessageHub.Instance.Publish(new MinorityForkDetected());
            }

            // Update current round number.
            LatestRoundNumber = _helper.GetCurrentRoundNumber(_chainId).Value;

            // Term number just changed from 1 to 2.
            if (LatestTermNumber == 1 && _helper.GetCurrentTermNumber(_chainId).Value == 2)
            {
                _firstTermChangedRoundNumber = LatestRoundNumber;
            }

            // Update current term number.
            LatestTermNumber = _helper.GetCurrentTermNumber(_chainId).Value;

            // Dispose previous observer.
            if (ConsensusDisposable != null)
            {
                ConsensusDisposable.Dispose();
                ConsensusDisposable = null;
                Logger.LogTrace("Disposed previous consensus observables list. Will reload new consnesus events.");
            }

            if (!_minerFlag)
            {
                Logger.LogTrace("This node became a miner.");
            }

            _minerFlag = true;

            // Subscribe consensus events here.
            ConsensusDisposable = ConsensusObserver.SubscribeMiningProcess(_helper.GetCurrentRoundInfo(_chainId));
        }

        private bool AmIContainedInCandidatesList()
        {
            return _helper.GetCandidates(_chainId).PublicKeys.Contains(_ownPubKey.ToHex());
        }

        public bool IsAlive()
        {
            var currentTime = DateTime.UtcNow;
            var currentRound = _helper.GetCurrentRoundInfo(_chainId);
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

            await _txHub.AddTransactionAsync(_chainId, tx, true);
        }

        public bool Shutdown()
        {
            return true;
        }

        private static bool MiningLocked()
        {
            return _lockNumber != 0;
        }

        private bool CanStartNextTerm()
        {
            if (_chainId.DumpBase58() == GlobalConfig.DefaultChainId)
            {
                return _helper.GetBlockchainAge(_chainId).Value / GlobalConfig.DaysEachTerm != LatestTermNumber - 1;
            }
            
            /*if (ChainConfig.Instance.ChainId == GlobalConfig.DefaultChainId &&
                _helper.TryToGetVictories(out var victories) &&
                victories.Count == GlobalConfig.BlockProducerNumber)
            {
                if (_firstTermChangedRoundNumber != 0)
                {
                    return (LatestRoundNumber - _firstTermChangedRoundNumber) / GlobalConfig.RoundsPerTerm + 2 !=
                           LatestTermNumber;
                }

                return true;
            }*/

            return false;
        }
    }
}