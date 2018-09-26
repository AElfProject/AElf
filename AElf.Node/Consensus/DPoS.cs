using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.ChainController.EventMessages;
using AElf.ChainController.TxMemPool;
using AElf.Common.ByteArrayHelpers;
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
using NServiceKit.Common.Extensions;

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
        private readonly IBlockChain _blockchain;
        private readonly IBlockSynchronizer _synchronizer;
        private readonly ILogger _logger;

        public static AElfDPoSHelper Helper;

        /// <summary>
        /// In Value and Out Value.
        /// </summary>
        private readonly Stack<Hash> _consensusData = new Stack<Hash>();

        private NodeKeyPair _nodeKeyPair;
        private Hash _contractAccountAddressHash;

        private int _flag;

        private AElfDPoSObserver AElfDPoSObserver => new AElfDPoSObserver(_logger,
            MiningWithInitializingAElfDPoSInformation,
            MiningWithPublishingOutValueAndSignature, PublishInValue, MiningWithUpdatingAElfDPoSInformation);

        public DPoS(IStateDictator stateDictator,
            ITxPoolService txPoolService,
            IMiner miner,
            IBlockChain blockchain,
            IBlockSynchronizer synchronizer,
            ILogger logger = null
        )
        {
            _stateDictator = stateDictator;
            _txPoolService = txPoolService;
            _miner = miner;
            _blockchain = blockchain;
            _synchronizer = synchronizer;
            _logger = logger;
        }
        
        public void Initialize(Hash contractAccountHash, ECKeyPair nodeKeyPair)
        {
            Helper = new AElfDPoSHelper(_stateDictator,
                ByteArrayHelpers.FromHexString(NodeConfig.Instance.ChainId), Miners, contractAccountHash, _logger);
            _nodeKeyPair = new NodeKeyPair(nodeKeyPair);
            _contractAccountAddressHash = contractAccountHash;

            var count = MinersConfig.Instance.Producers.Count;
            Globals.BlockProducerNumber = count;
            _logger?.Trace("Block Producer nodes count:" + count);
            if (Globals.BlockProducerNumber == 1)
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
            if (isMining)
                return;

            isMining = true;

            if (!Miners.Nodes.Contains(_nodeKeyPair.Address.ToHex().RemoveHexPrefix()))
            {
                return;
            }

            if (NodeConfig.Instance.ConsensusInfoGenerater && !await Helper.HasGenerated())
            {
                Globals.IsConsensusGenerator = true;
                AElfDPoSObserver.Initialization();
                return;
            }

            Helper.SyncMiningInterval();
            _logger?.Trace($"Set AElf DPoS mining interval to: {Globals.AElfDPoSMiningInterval} ms.");


            if (Helper.CanRecoverDPoSInformation())
            {
                AElfDPoSObserver.RecoverMining();
            }
        }

        private async Task<IBlock> Mine()
        {
            var res = Interlocked.CompareExchange(ref _flag, 1, 0);
            if (res == 1)
                return null;
            try
            {
                _logger?.Trace($"Mine - Entered mining {res}");
                _stateDictator.ChainId = ByteArrayHelpers.FromHexString(NodeConfig.Instance.ChainId);
                _stateDictator.BlockProducerAccountAddress = _nodeKeyPair.Address;
                _stateDictator.BlockHeight = await _blockchain.GetCurrentBlockHeightAsync();

                var block = await _miner.Mine(Helper.GetCurrentRoundInfo());

                await _stateDictator.SetBlockHashAsync(block.GetHash());
                await _stateDictator.SetStateHashAsync(block.GetHash());

                _synchronizer.IncrementChainHeight();

                //Update DPoS observables.
                try
                {
                    await Update();
                }
                catch (Exception e)
                {
                    _logger?.Error(e, "Somehow failed to update DPoS observables. Will recover soon.");
                    //In case just config one node to produce blocks.
                    await RecoverMining();
                }
                /*if(!block.Header.IndexedInfo.IsEmpty())
                    _logger?.Debug($"Indexed side chain info in main block {block.Header.Index}:\n{block.Header.GetIndexedSideChainBlcokInfo()}");*/
                return block;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
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
            var bn = await _blockchain.GetCurrentBlockHeightAsync();
            bn = bn > 4 ? bn - 4 : 0;
            var bh = bn == 0 ? Hash.Genesis : (await _blockchain.GetHeaderByHeightAsync(bn)).GetHash();
            var bhPref = bh.Value.Where((x, i) => i < 4).ToArray();
            var tx = new Transaction
            {
                From = _nodeKeyPair.Address,
                To = _contractAccountAddressHash,
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
            var signature = signer.Sign(_nodeKeyPair, tx.GetHash().GetHashBytes());

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
                new SInt32Value {Value = Globals.AElfDPoSMiningInterval}.ToByteArray(),
                logLevel.ToByteArray()
            };
            var txToInitializeAElfDPoS = await GenerateTransactionAsync("InitializeAElfDPoS", parameters);
            await BroadcastTransaction(txToInitializeAElfDPoS);

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
                _consensusData.Push(inValue.CalculateHash());
            }

            var currentRoundNumber = Helper.CurrentRoundNumber;
            var signature = Hash.Default;
            if (currentRoundNumber.Value > 1)
            {
                _logger?.Trace("In value used for generating signature: " + inValue.ToHex());
                signature = Helper.CalculateSignature(inValue);
            }

            var parameters = new List<byte[]>
            {
                Helper.CurrentRoundNumber.ToByteArray(),
                new StringValue {Value = _nodeKeyPair.Address.ToHex().RemoveHexPrefix()}.ToByteArray(),
                _consensusData.Pop().ToByteArray(),
                signature.ToByteArray(),
                new Int64Value {Value = Helper.GetCurrentRoundInfo().RoundId}.ToByteArray()
            };

            var txToPublishOutValueAndSignature =
                await GenerateTransactionAsync("PublishOutValueAndSignature", parameters);

            await BroadcastTransaction(txToPublishOutValueAndSignature);

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
                new StringValue {Value = _nodeKeyPair.Address.ToHex().RemoveHexPrefix()}.ToByteArray(),
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

            await Mine();
        }

        public async Task Update()
        {
            Helper.LogDPoSInformation(await _blockchain.GetCurrentBlockHeightAsync());

            if (ConsensusMemory == Helper.CurrentRoundNumber.Value)
                return;

            // Dispose previous observer.
            if (ConsensusDisposable != null)
            {
                ConsensusDisposable.Dispose();
                _logger?.Trace("Disposed previous consensus observables list.");
            }

            // Update observer.
            var address = _nodeKeyPair.Address.ToHex().RemoveHexPrefix();
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

        //TODO: improve
        public bool IsAlive()
        {
            var currentTime = DateTime.UtcNow;
            var currentRound = Helper.GetCurrentRoundInfo();
            var startTimeSlot = currentRound.BlockProducers.First(bp => bp.Value.Order == 1).Value.TimeSlot.ToDateTime();

            var endTimeSlot =
                startTimeSlot.AddMilliseconds(Globals.BlockProducerNumber * Globals.AElfDPoSMiningInterval * 2);

            return currentTime >
                   startTimeSlot.AddMilliseconds(-Globals.BlockProducerNumber * Globals.AElfDPoSMiningInterval) &&
                   currentTime < endTimeSlot;
        }

        private async Task BroadcastTransaction(Transaction tx)
        {
            if (tx.Type == TransactionType.DposTransaction)
            {
                _logger?.Trace($"A DPoS tx has been generated: {tx.GetHash().ToHex()} - {tx.MethodName} from {tx.From.ToHex()}");
            }
            
            if (tx.From.Equals(_nodeKeyPair.Address))
                _logger?.Trace("Try to insert DPoS transaction to pool: " + tx.GetHash().ToHex() + ", threadId: " +
                               Thread.CurrentThread.ManagedThreadId);
            try
            {
                var result = await _txPoolService.AddTxAsync(tx);
                if (result == TxValidation.TxInsertionAndBroadcastingError.Success)
                    MessageHub.Instance.Publish(new TransactionAddedToPool(tx));
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