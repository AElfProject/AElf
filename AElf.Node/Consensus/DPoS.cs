using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.ChainController.EventMessages;
using AElf.ChainController.TxMemPool;
using AElf.Common.ByteArrayHelpers;
using AElf.Common.Extensions;
using AElf.Configuration;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Consensus;
using AElf.Kernel.Node.Protocol;
using AElf.Miner.Miner;
using AElf.Node;
using AElf.Node.AElfChain;
using AElf.Node.Protocol;
using AElf.SmartContract;
using AElf.Types.CSharp;
using Easy.MessageHub;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Mono.Cecil.Cil;
using NLog;

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
        private IDisposable ConsensusDisposable { get; set; }
        
        private bool isMining;

        private readonly IStateDictator _stateDictator;
        private readonly IAccountContextService _accountContextService;
        private readonly ITxPoolService _txPoolService;
        private readonly IP2P _p2p;
        private readonly IMiner _miner;
        private readonly IBlockChain _blockchain;
        private readonly IBlockSynchronizer _syncer;
        private readonly ILogger _logger;

        private AElfDPoSHelper _dposHelpers;
        
        /// <summary>
        /// In Value and Out Value.
        /// </summary>
        private readonly Stack<Hash> _consensusData = new Stack<Hash>();
        private bool _incrementIdNeedToAddOne;

        private NodeKeyPair _nodeKeyPair;
        private Hash _contractAccountAddressHash;
        
        private int _flag;

        private AElfDPoSObserver AElfDPoSObserver => new AElfDPoSObserver(_logger,
            MiningWithInitializingAElfDPoSInformation,
            MiningWithPublishingOutValueAndSignature, PublishInValue, MiningWithUpdatingAElfDPoSInformation);

        public DPoS(IStateDictator stateDictator, 
            IAccountContextService accountContextService, 
            ITxPoolService txPoolService, 
            IP2P p2p,
            IMiner miner, 
            IBlockChain blockchain,
            IBlockSynchronizer syncer,
            ILogger logger = null
        )
        {
            _stateDictator = stateDictator;
            _accountContextService = accountContextService;
            _txPoolService = txPoolService;
            _p2p = p2p;
            _miner = miner;
            _blockchain = blockchain;
            _syncer = syncer;
            _logger = logger;
        }

        public void Initialize(Hash contractAccountHash, ECKeyPair nodeKeyPair)
        {
            _dposHelpers = new AElfDPoSHelper(_stateDictator, ByteArrayHelpers.FromHexString(NodeConfig.Instance.ChainId), Miners, contractAccountHash, _logger);
            _nodeKeyPair = new NodeKeyPair(nodeKeyPair);
            _contractAccountAddressHash = contractAccountHash;
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

                Globals.BlockProducerNumber = miners.Nodes.Count;
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

            if (NodeConfig.Instance.ConsensusInfoGenerater && !await _dposHelpers.HasGenerated())
            {
                AElfDPoSObserver.Initialization();
                return;
            }

            _dposHelpers.SyncMiningInterval();
            _logger?.Trace($"Set AElf DPoS mining interval to: {Globals.AElfDPoSMiningInterval} ms.");


            if (_dposHelpers.CanRecoverDPoSInformation())
            {
                AElfDPoSObserver.RecoverMining();
            }
        }

        private async Task<IBlock> Mine(bool initial = false)
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

                var block = await _miner.Mine(Globals.AElfDPoSMiningInterval * 9 / 10, initial);

                await _stateDictator.SetBlockHashAsync(block.GetHash());
                await _stateDictator.SetStateHashAsync(block.GetHash());

                _syncer.IncrementChainHeight();

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

        /// <summary>
        /// return default incrementId for one address
        /// </summary>
        /// <param name="accountAddress"></param>
        /// <returns></returns>
        private async Task<ulong> GetIncrementId(Hash accountAddress)
        {
            try
            {
                var isBlockProducer = accountAddress.Equals(_nodeKeyPair.Address) ||
                              _dposHelpers.Miners.Nodes.Contains(accountAddress.ToHex().RemoveHexPrefix());
                var chainId = ByteArrayHelpers.FromHexString(NodeConfig.Instance.ChainId);
                
                var idInDB = (await _accountContextService.GetAccountDataContext(accountAddress, chainId)).IncrementId;
                var idInPool = _txPoolService.GetIncrementId(accountAddress, isBlockProducer);

                return Math.Max(idInDB, idInPool);
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Failed to get increment id.");
                return 0;
            }
        }

        private Transaction GenerateTransaction(string methodName, IReadOnlyList<byte[]> parameters, ulong incrementIdOffset = 0)
        {
            var tx = new Transaction
            {
                From = _nodeKeyPair.Address,
                To = _contractAccountAddressHash,
                IncrementId = GetIncrementId(_nodeKeyPair.Address).Result + incrementIdOffset,
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
            }

            var signer = new ECSigner();
            var signature = signer.Sign(_nodeKeyPair, tx.GetHash().GetHashBytes());

            // Update the signature
            tx.R = ByteString.CopyFrom(signature.R);
            tx.S = ByteString.CopyFrom(signature.S);

            return tx;
        }
        
        private async Task MiningWithInitializingAElfDPoSInformation()
        {
            var logLevel = new Int32Value {Value = LogManager.GlobalThreshold.Ordinal};
            var parameters = new List<byte[]>
            {
                Miners.ToByteArray(),
                _dposHelpers.GenerateInfoForFirstTwoRounds().ToByteArray(),
                new SInt32Value {Value = Globals.AElfDPoSMiningInterval}.ToByteArray(),
                logLevel.ToByteArray()
            };
            _logger?.Trace($"Set AElf DPoS mining interval to: {Globals.AElfDPoSMiningInterval} ms");
            var txToInitializeAElfDPoS = GenerateTransaction("InitializeAElfDPoS", parameters);
            await BroadcastTransaction(txToInitializeAElfDPoS);

            var block = await Mine(true);
            //await _p2p.BroadcastBlock(block);
        }

        private async Task MiningWithPublishingOutValueAndSignature()
        {
            var inValue = Hash.Generate();
            if (_consensusData.Count <= 0)
            {
                _consensusData.Push(inValue.CalculateHash());
                _consensusData.Push(inValue);
            }

            var currentRoundNumber = _dposHelpers.CurrentRoundNumber;
            var signature = Hash.Default;
            if (currentRoundNumber.Value > 1)
            {
                signature = _dposHelpers.CalculateSignature(inValue);
            }

            var parameters = new List<byte[]>
            {
                _dposHelpers.CurrentRoundNumber.ToByteArray(),
                new StringValue {Value = _nodeKeyPair.Address.ToHex().RemoveHexPrefix()}.ToByteArray(),
                _consensusData.Pop().ToByteArray(),
                signature.ToByteArray()
            };

            var txToPublishOutValueAndSignature = GenerateTransaction("PublishOutValueAndSignature", parameters);

            await BroadcastTransaction(txToPublishOutValueAndSignature);

            var block = await Mine();
            //await _p2p.BroadcastBlock(block);
        }

        private async Task PublishInValue()
        {
            if (_consensusData.Count <= 0)
            {
                _incrementIdNeedToAddOne = false;
                return;
            }

            _incrementIdNeedToAddOne = true;

            var currentRoundNumber = _dposHelpers.CurrentRoundNumber;

            var parameters = new List<byte[]>
            {
                currentRoundNumber.ToByteArray(),
                new StringValue {Value = _nodeKeyPair.Address.ToHex().RemoveHexPrefix()}.ToByteArray(),
                _consensusData.Pop().ToByteArray()
            };

            var txToPublishInValue = GenerateTransaction("PublishInValue", parameters);
            await BroadcastTransaction(txToPublishInValue);
        }

        private async Task MiningWithUpdatingAElfDPoSInformation()
        {
            var extraBlockResult = _dposHelpers.ExecuteTxsForExtraBlock();

            var parameters = new List<byte[]>
            {
                extraBlockResult.Item1.ToByteArray(),
                extraBlockResult.Item2.ToByteArray(),
                extraBlockResult.Item3.ToByteArray()
            };

            var txForExtraBlock = GenerateTransaction(
                "UpdateAElfDPoS",
                parameters,
                _incrementIdNeedToAddOne ? (ulong) 1 : 0);

            await BroadcastTransaction(txForExtraBlock);

            var block = await Mine();
            //await _p2p.BroadcastBlock(block);
        }

        public async Task Update()
        {
            _dposHelpers.LogDPoSInformation(await _blockchain.GetCurrentBlockHeightAsync());
            
            if (ConsensusMemory == _dposHelpers.CurrentRoundNumber.Value)
                return;
            
            // Dispose previous observer.
            if (ConsensusDisposable != null)
            {
                ConsensusDisposable.Dispose();
                _logger?.Trace("Disposed previous consensus observables list.");
            }

            // Update observer.
            var address = _nodeKeyPair.Address.ToHex().RemoveHexPrefix();
            var blockProducerInfoOfCurrentRound = _dposHelpers[address];
            ConsensusDisposable = AElfDPoSObserver.SubscribeAElfDPoSMiningProcess(blockProducerInfoOfCurrentRound, 
                _dposHelpers.ExtraBlockTimeslot);

            // Update current round number.
            ConsensusMemory = _dposHelpers.CurrentRoundNumber.Value;
        }

        public async Task RecoverMining()
        {
            AElfDPoSObserver.RecoverMining();
            await Task.CompletedTask;
        }

        private async Task BroadcastTransaction(Transaction tx)
        {
            if(tx.From.Equals(_nodeKeyPair.Address))
                _logger?.Trace("Try to insert DPoS transaction to pool: " + tx.GetHash().ToHex() + ", threadId: " +
                               Thread.CurrentThread.ManagedThreadId);
            try
            {
                if (await _txPoolService.AddTxAsync(tx) == TxValidation.TxInsertionAndBroadcastingError.Success)
                    MessageHub.Instance.Publish(new TransactionAddedToPool(tx));
            }
            catch (Exception e)
            {
                _logger?.Trace("Transaction insertion failed: {0},\n{1}", e.Message, tx.GetTransactionInfo());
            }
        } 
    }
}