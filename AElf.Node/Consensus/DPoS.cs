using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.ChainController.EventMessages;
using AElf.ChainController.TxMemPool;
using AElf.Common;
using AElf.Configuration;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Consensus;
using AElf.Miner.Miner;
using AElf.Node;
using AElf.Node.AElfChain;
using AElf.Node.Protocol;
using AElf.SmartContract;
using AElf.Types.CSharp;
using Easy.MessageHub;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using NLog;
using AElf.Common;
using AElf.Node.EventMessages;

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

        public static IDisposable ConsensusDisposable { get; set; }

        private bool isMining;

        private readonly IStateDictator _stateDictator;
        private readonly ITxPoolService _txPoolService;
        private readonly IMiner _miner;
        private readonly IChainService _chainService;
        
        private IBlockChain _blockChain;
        private IBlockChain BlockChain => _blockChain ?? (_blockChain =
                                              _chainService.GetBlockChain(
                                                  Hash.LoadHex(NodeConfig.Instance.ChainId)));
        
        private readonly ILogger _logger;

        public static AElfDPoSHelper Helper;

        /// <summary>
        /// In Value and Out Value.
        /// </summary>
        private readonly Stack<Hash> _consensusData = new Stack<Hash>();

        private readonly NodeKeyPair _nodeKeyPair = new NodeKeyPair(NodeConfig.Instance.ECKeyPair);

        public Address ContractAddress => AddressHelpers.GetSystemContractAddress(
            Hash.LoadHex(NodeConfig.Instance.ChainId),
            SmartContractType.AElfDPoS.ToString());

        private int _flag;

        private AElfDPoSObserver AElfDPoSObserver => new AElfDPoSObserver(MiningWithInitializingAElfDPoSInformation,
            MiningWithPublishingOutValueAndSignature, PublishInValue, MiningWithUpdatingAElfDPoSInformation);

        public DPoS(IStateDictator stateDictator, ITxPoolService txPoolService, IMiner miner,
            IChainService chainService)
        {
            _stateDictator = stateDictator;
            _txPoolService = txPoolService;
            _miner = miner;
            _chainService = chainService;

            _logger = LogManager.GetLogger(nameof(DPoS));

            Helper = new AElfDPoSHelper(_stateDictator, Hash.LoadHex(NodeConfig.Instance.ChainId), Miners,
                ContractAddress);

            var count = MinersConfig.Instance.Producers.Count;

            GlobalConfig.BlockProducerNumber = count;
            GlobalConfig.BlockNumberOfEachRound = count + 1;

            _logger?.Trace("Block Producer nodes count:" + GlobalConfig.BlockProducerNumber);
            _logger?.Trace("Blocks of one round:" + GlobalConfig.BlockNumberOfEachRound);

            if (GlobalConfig.BlockProducerNumber == 1 && NodeConfig.Instance.IsMiner)
            {
                AElfDPoSObserver.RecoverMining();
            }
        }

        private static Miners Miners
        {
            get
            {
                var dict = MinersConfig.Instance.Producers;
                var miners = new Miners();

                foreach (var bp in dict.Values)
                {
                    var b = bp["address"].RemoveHexPrefix();
                    miners.Nodes.Add(b);
                }

                return miners;
            }
        }

        public async Task Start()
        {
            if (ConsensusDisposable != null)
            {
                Recover();
                return;
            }
            
            if (isMining)
                return;

            isMining = true;

            // Check whether this node contained BP list.
            if (!Miners.Nodes.Contains(_nodeKeyPair.Address.DumpHex().RemoveHexPrefix()))
            {
                return;
            }

            if (NodeConfig.Instance.ConsensusInfoGenerator && !await Helper.HasGenerated())
            {
                GlobalConfig.IsConsensusGenerator = true;
                AElfDPoSObserver.Initialization();
                return;
            }

            Helper.SyncMiningInterval();
            _logger?.Trace($"Set AElf DPoS mining interval to: {GlobalConfig.AElfDPoSMiningInterval} ms.");

            if (Helper.CanRecoverDPoSInformation())
            {
                AElfDPoSObserver.RecoverMining();
            }
        }

        public void Stop()
        {
            ConsensusDisposable?.Dispose();
            _logger?.Trace("Mining stopped. Disposed previous consensus observables list.");
        }

        public void Hang()
        {
            Interlocked.CompareExchange(ref _flag, 1, 0);
        }

        public void Recover()
        {
            Interlocked.CompareExchange(ref _flag, 0, 1);
        }

        private async Task<IBlock> Mine()
        {
            var res = Interlocked.CompareExchange(ref _flag, 1, 0);
            if (res == 1)
                return null;
            try
            {
                MessageHub.Instance.Publish(new SyncUnfinishedBlock(await BlockChain.GetCurrentBlockHeightAsync()));
                
                _logger?.Trace($"Mine - Entered mining {res}");
                
                // Prepare the state of new block.
                _stateDictator.ChainId = Hash.LoadHex(NodeConfig.Instance.ChainId);
                _stateDictator.BlockProducerAccountAddress = _nodeKeyPair.Address;
                _stateDictator.BlockHeight = await BlockChain.GetCurrentBlockHeightAsync();

                var block = await _miner.Mine(Helper.GetCurrentRoundInfo());

                await _stateDictator.SetMap(block.GetHash());

                return block;
            }
            catch (Exception e)
            {
                _logger?.Trace(e.Message);
                return null;
            }
            finally
            {
                // release lock
                var b = Interlocked.CompareExchange(ref _flag, 0, 1);
                _logger?.Trace($"Mine - Leaving mining {b}");
            }
        }

        private async Task<Transaction> GenerateTransactionAsync(string methodName, IReadOnlyList<byte[]> parameters)
        {
            var bn = await BlockChain.GetCurrentBlockHeightAsync();
            bn = bn > 4 ? bn - 4 : 0;
            var bh = bn == 0 ? Hash.Genesis : (await BlockChain.GetHeaderByHeightAsync(bn)).GetHash();
            var bhPref = bh.Value.Where((x, i) => i < 4).ToArray();
            var tx = new Transaction
            {
                From = _nodeKeyPair.Address,
                To = ContractAddress,
                RefBlockNumber = bn,
                RefBlockPrefix = ByteString.CopyFrom(bhPref),
                MethodName = methodName,
                P = ByteString.CopyFrom(_nodeKeyPair.NonCompressedEncodedPublicKey),
                Type = TransactionType.DposTransaction
            };

            switch (parameters.Count)
            {
                case 2:
                    tx.Params = ByteString.CopyFrom(ParamsPacker.Pack(parameters[0], parameters[1]));
                    break;
                case 3:
                    tx.Params = ByteString.CopyFrom(ParamsPacker.Pack(parameters[0], parameters[1], parameters[2]));
                    break;
                case 4:
                    tx.Params = ByteString.CopyFrom(ParamsPacker.Pack(parameters[0], parameters[1], parameters[2],
                        parameters[3]));
                    break;
                case 5:
                    tx.Params = ByteString.CopyFrom(ParamsPacker.Pack(parameters[0], parameters[1], parameters[2],
                        parameters[3], parameters[4]));
                    break;
            }

            var signer = new ECSigner();
            var signature = signer.Sign(_nodeKeyPair, tx.GetHash().DumpByteArray());

            // Update the signature
            tx.R = ByteString.CopyFrom(signature.R);
            tx.S = ByteString.CopyFrom(signature.S);

            return tx;
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
            var logLevel = new Int32Value {Value = LogManager.GlobalThreshold.Ordinal};
            var parameters = new List<byte[]>
            {
                Miners.ToByteArray(),
                Helper.GenerateInfoForFirstTwoRounds().ToByteArray(),
                new SInt32Value {Value = GlobalConfig.AElfDPoSMiningInterval}.ToByteArray(),
                logLevel.ToByteArray()
            };
            var txToInitializeAElfDPoS = await GenerateTransactionAsync("InitializeAElfDPoS", parameters);
            await BroadcastTransaction(txToInitializeAElfDPoS);
            
            MessageHub.Instance.Publish(new ConsensusStateChanged(ConsensusBehavior.InitializeAElfDPoS));

            await Mine();
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
            var inValue = Hash.Generate();
            if (_consensusData.Count <= 0)
            {
                _consensusData.Push(inValue);
                _consensusData.Push(Hash.FromMessage(inValue));
            }

            var currentRoundNumber = Helper.CurrentRoundNumber;
            var signature = Hash.Default;
            if (currentRoundNumber.Value > 1)
            {
                _logger?.Trace("In value used for generating signature: " + inValue.DumpHex());
                signature = Helper.CalculateSignature(inValue);
            }

            var parameters = new List<byte[]>
            {
                Helper.CurrentRoundNumber.ToByteArray(),
                new StringValue {Value = _nodeKeyPair.Address.DumpHex().RemoveHexPrefix()}.ToByteArray(),
                _consensusData.Pop().ToByteArray(),
                signature.ToByteArray(),
                new Int64Value {Value = Helper.GetCurrentRoundInfo().RoundId}.ToByteArray()
            };

            var txToPublishOutValueAndSignature =
                await GenerateTransactionAsync("PublishOutValueAndSignature", parameters);

            await BroadcastTransaction(txToPublishOutValueAndSignature);
            
            MessageHub.Instance.Publish(new ConsensusStateChanged(ConsensusBehavior.PublishOutValueAndSignature));

            await Mine();
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
            var currentRoundNumber = Helper.CurrentRoundNumber;

            var parameters = new List<byte[]>
            {
                currentRoundNumber.ToByteArray(),
                new StringValue {Value = _nodeKeyPair.Address.DumpHex().RemoveHexPrefix()}.ToByteArray(),
                _consensusData.Pop().ToByteArray(),
                new Int64Value {Value = Helper.GetCurrentRoundInfo().RoundId}.ToByteArray()
            };

            var txToPublishInValue = await GenerateTransactionAsync("PublishInValue", parameters);
            await BroadcastTransaction(txToPublishInValue);
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
            var extraBlockResult = Helper.ExecuteTxsForExtraBlock();

            var parameters = new List<byte[]>
            {
                extraBlockResult.Item1.ToByteArray(),
                extraBlockResult.Item2.ToByteArray(),
                extraBlockResult.Item3.ToByteArray(),
                new Int64Value {Value = Helper.GetCurrentRoundInfo().RoundId}.ToByteArray()
            };

            var txForExtraBlock = await GenerateTransactionAsync("UpdateAElfDPoS", parameters);

            await BroadcastTransaction(txForExtraBlock);

            MessageHub.Instance.Publish(new ConsensusStateChanged(ConsensusBehavior.UpdateAElfDPoS));

            await Mine();
        }

        public async Task Update()
        {
            Helper.LogDPoSInformation(await BlockChain.GetCurrentBlockHeightAsync());

            if (ConsensusMemory == Helper.CurrentRoundNumber.Value)
                return;

            // Dispose previous observer.
            if (ConsensusDisposable != null)
            {
                ConsensusDisposable.Dispose();
                _logger?.Trace("Disposed previous consensus observables list. Will update DPoS information.");
            }

            // Update observer.
            var address = _nodeKeyPair.Address.DumpHex().RemoveHexPrefix();
            var miners = Helper.Miners;
            if (!miners.Nodes.Contains(address))
            {
                return;
            }
            var blockProducerInfoOfCurrentRound = Helper[address];
            ConsensusDisposable = AElfDPoSObserver.SubscribeAElfDPoSMiningProcess(blockProducerInfoOfCurrentRound,
                Helper.ExtraBlockTimeSlot);

            // Update current round number.
            ConsensusMemory = Helper.CurrentRoundNumber.Value;
        }

        public async Task RecoverMining()
        {
            AElfDPoSObserver.RecoverMining();
            await Task.CompletedTask;
        }

        public bool IsAlive()
        {
            var currentTime = DateTime.UtcNow;
            var currentRound = Helper.GetCurrentRoundInfo();
            var startTimeSlot = currentRound.BlockProducers.First(bp => bp.Value.Order == 1).Value.TimeSlot.ToDateTime();

            var endTimeSlot =
                startTimeSlot.AddMilliseconds(GlobalConfig.BlockProducerNumber * GlobalConfig.AElfDPoSMiningInterval * 2);

            return currentTime >
                   startTimeSlot.AddMilliseconds(-GlobalConfig.BlockProducerNumber * GlobalConfig.AElfDPoSMiningInterval) ||
                   currentTime < endTimeSlot.AddMilliseconds(GlobalConfig.AElfDPoSMiningInterval);
        }

        private async Task BroadcastTransaction(Transaction tx)
        {
            if (tx.Type == TransactionType.DposTransaction)
            {
                _logger?.Trace($"A DPoS tx has been generated: {tx.GetHash().DumpHex()} - {tx.MethodName} from {tx.From.DumpHex()}");
            }
            
            if (tx.From.Equals(_nodeKeyPair.Address))
                _logger?.Trace("Try to insert DPoS transaction to pool: " + tx.GetHash().DumpHex() + ", threadId: " +
                               Thread.CurrentThread.ManagedThreadId);
            try
            {
                var result = await _txPoolService.AddTxAsync(tx);
                if (result == TxValidation.TxInsertionAndBroadcastingError.Success)
                {
                    _logger?.Trace("Tx added to the pool");
                    MessageHub.Instance.Publish(new TransactionAddedToPool(tx));
                }
                else
                {
                    _logger?.Trace("Failed to insert tx: " + result);
                }
            }
            catch (Exception e)
            {
                _logger?.Debug("Transaction insertion failed: {0},\n{1}", e.Message, tx.GetTransactionInfo());
            }
        }
    }
}