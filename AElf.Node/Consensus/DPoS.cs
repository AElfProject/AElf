using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.Common.ByteArrayHelpers;
using AElf.Common.Extensions;
using AElf.Kernel.Types;
using AElf.Configuration;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Consensus;
using AElf.SmartContract;
using AElf.Types.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using NLog;

namespace AElf.Kernel.Node
{
    // ReSharper disable once InconsistentNaming
    public class DPoS : IConsensus
    {
        private readonly ILogger _logger;
        private bool IsMining { get; set; }

        private ECKeyPair NodeKeyPair
        {
            get => Node.NodeKeyPair;
        }

        private readonly IAccountContextService _accountContextService;
        private readonly ITxPoolService _txPoolService;
        private readonly IP2P _p2p;
        public ulong ConsensusMemory { get; set; }
        public IDisposable ConsensusDisposable { get; set; }

        // ReSharper disable once InconsistentNaming
        public AElfDPoSHelper DPoSHelper { get; }
        private MainChainNode Node { get; }
        private readonly Stack<Hash> _consensusData = new Stack<Hash>();
        private bool _incrementIdNeedToAddOne;

        // ReSharper disable once InconsistentNaming
        public AElfDPoSObserver AElfDPoSObserver => new AElfDPoSObserver(_logger,
            MiningWithInitializingAElfDPoSInformation,
            MiningWithPublishingOutValueAndSignature, PublishInValue, MiningWithUpdatingAElfDPoSInformation);

        public DPoS(ILogger logger, MainChainNode node,
            IWorldStateDictator worldStateDictator, IAccountContextService accountContextService,
            ITxPoolService txPoolService,
            IP2P p2p
        )
        {
            _logger = logger;
            _accountContextService = accountContextService;
            _p2p = p2p;
            _txPoolService = txPoolService;
            Node = node;
            DPoSHelper = new AElfDPoSHelper(worldStateDictator, NodeKeyPair, ByteArrayHelpers.FromHexString(NodeConfig.Instance.ChainId), BlockProducers,
                Node.ContractAccountHash, _logger);
        }

        public BlockProducer BlockProducers
        {
            get
            {
                var dict = MinersConfig.Instance.Producers;
                var blockProducers = new BlockProducer();

                foreach (var bp in dict.Values)
                {
                    var b = bp["address"].RemoveHexPrefix();
                    blockProducers.Nodes.Add(b);
                }

                Globals.BlockProducerNumber = blockProducers.Nodes.Count;
                return blockProducers;
            }
        }

        public async Task Start()
        {
            if (IsMining)
                return;

            IsMining = true;

            if (!BlockProducers.Nodes.Contains(NodeKeyPair.GetAddress().ToHex().RemoveHexPrefix()))
            {
                return;
            }

            if (NodeConfig.Instance.ConsensusInfoGenerater && !await DPoSHelper.HasGenerated())
            {
                AElfDPoSObserver.Initialization();
                return;
            }

            DPoSHelper.SyncMiningInterval();
            _logger?.Trace($"Set AElf DPoS mining interval: {Globals.AElfDPoSMiningInterval} ms.");


            if (DPoSHelper.CanRecoverDPoSInformation())
            {
                AElfDPoSObserver.RecoverMining();
            }
        }

        // ReSharper disable once InconsistentNaming
        public async Task MiningWithInitializingAElfDPoSInformation()
        {
            var parameters = new List<byte[]>
            {
                BlockProducers.ToByteArray(),
                DPoSHelper.GenerateInfoForFirstTwoRounds().ToByteArray(),
                new Int32Value {Value = Globals.AElfDPoSMiningInterval}.ToByteArray()
            };
            _logger?.Trace($"Set AElf DPoS mining interval: {Globals.AElfDPoSMiningInterval} ms");
            // ReSharper disable once InconsistentNaming
            var txToInitializeAElfDPoS = GenerateTransaction("InitializeAElfDPoS", parameters);
            await BroadcastTransaction(txToInitializeAElfDPoS);

            var block = await Node.Mine();
            await _p2p.BroadcastBlock(block);
        }

        /// <summary>
        /// return default incrementId for one address
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        public async Task<ulong> GetIncrementId(Hash addr)
        {
            try
            {
                bool isDPoS = addr.Equals(NodeKeyPair.GetAddress()) ||
                              DPoSHelper.BlockProducer.Nodes.Contains(addr.ToHex().RemoveHexPrefix());

                // ReSharper disable once InconsistentNaming
                var idInDB = (await _accountContextService.GetAccountDataContext(addr, ByteArrayHelpers.FromHexString(NodeConfig.Instance.ChainId)))
                    .IncrementId;
                _logger?.Log(LogLevel.Debug, $"Trying to get increment id, {isDPoS}");
                var idInPool = _txPoolService.GetIncrementId(addr, isDPoS);
                _logger?.Log(LogLevel.Debug, $"End Trying to get increment id, {isDPoS}");

                return Math.Max(idInDB, idInPool);
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Failed to get increment id.");
                return 0;
            }
        }

        // ReSharper disable once InconsistentNaming
        private ITransaction GenerateTransaction(string methodName, IReadOnlyList<byte[]> parameters,
            ulong incrementIdOffset = 0)
        {
            var tx = new Transaction
            {
                From = NodeKeyPair.GetAddress(),
                To = Node.ContractAccountHash,
                IncrementId = GetIncrementId(NodeKeyPair.GetAddress()).Result + incrementIdOffset,
                MethodName = methodName,
                P = ByteString.CopyFrom(NodeKeyPair.PublicKey.Q.GetEncoded()),
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
            var signature = signer.Sign(NodeKeyPair, tx.GetHash().GetHashBytes());

            // Update the signature
            tx.R = ByteString.CopyFrom(signature.R);
            tx.S = ByteString.CopyFrom(signature.S);

            return tx;
        }

        public async Task MiningWithPublishingOutValueAndSignature()
        {
            var inValue = Hash.Generate();
            if (_consensusData.Count <= 0)
            {
                _consensusData.Push(inValue.CalculateHash());
                _consensusData.Push(inValue);
            }

            var currentRoundNumber = DPoSHelper.CurrentRoundNumber;
            var signature = Hash.Default;
            if (currentRoundNumber.Value > 1)
            {
                signature = DPoSHelper.CalculateSignature(inValue);
            }

            var parameters = new List<byte[]>
            {
                DPoSHelper.CurrentRoundNumber.ToByteArray(),
                new StringValue {Value = NodeKeyPair.GetAddress().ToHex().RemoveHexPrefix()}.ToByteArray(),
                _consensusData.Pop().ToByteArray(),
                signature.ToByteArray()
            };

            var txToPublishOutValueAndSignature = GenerateTransaction("PublishOutValueAndSignature", parameters);

            await BroadcastTransaction(txToPublishOutValueAndSignature);

            var block = await Node.Mine();
            await _p2p.BroadcastBlock(block);
        }

        public async Task PublishInValue()
        {
            if (_consensusData.Count <= 0)
            {
                _incrementIdNeedToAddOne = false;
                return;
            }

            _incrementIdNeedToAddOne = true;

            var currentRoundNumber = DPoSHelper.CurrentRoundNumber;

            var parameters = new List<byte[]>
            {
                currentRoundNumber.ToByteArray(),
                new StringValue {Value = NodeKeyPair.GetAddress().ToHex().RemoveHexPrefix()}.ToByteArray(),
                _consensusData.Pop().ToByteArray()
            };

            var txToPublishInValue = GenerateTransaction("PublishInValue", parameters);
            await BroadcastTransaction(txToPublishInValue);
        }

        // ReSharper disable once InconsistentNaming
        public async Task MiningWithUpdatingAElfDPoSInformation()
        {
            _logger?.Log(LogLevel.Debug, "MiningWithUpdatingAElf..");
            var extraBlockResult = await DPoSHelper.ExecuteTxsForExtraBlock();
            _logger?.Log(LogLevel.Debug, "End MiningWithUpdatingAElf..");

            var parameters = new List<byte[]>
            {
                extraBlockResult.Item1.ToByteArray(),
                extraBlockResult.Item2.ToByteArray(),
                extraBlockResult.Item3.ToByteArray()
            };
            _logger?.Log(LogLevel.Debug, "Generating transaction..");

            var txForExtraBlock = GenerateTransaction(
                "UpdateAElfDPoS",
                parameters,
                _incrementIdNeedToAddOne ? (ulong) 1 : 0);
            _logger?.Log(LogLevel.Debug, "End Generating transaction..");

            await BroadcastTransaction(txForExtraBlock);

            var block = await Node.Mine();
            await _p2p.BroadcastBlock(block);
        }

        public async Task Update()
        {
            var blockchain = Node.BlockChain;
            var hash = await blockchain.GetCurrentBlockHashAsync();
            var header = (BlockHeader) await blockchain.GetHeaderByHashAsync(hash);
            //Do DPoS log
            _logger?.Trace(await DPoSHelper.GetDPoSInfo(header.Index));
            _logger?.Trace("Log dpos information - End");

            if (ConsensusMemory == DPoSHelper.CurrentRoundNumber.Value)
                return;
            //Dispose previous observer.
            if (ConsensusDisposable != null)
            {
                ConsensusDisposable.Dispose();
                _logger?.Trace("Disposed previous consensus observables list.");
            }
            else
            {
                _logger?.Trace("For now the consensus observables list is null.");
            }

            //Update observer.
            var blockProducerInfoOfCurrentRound =
                DPoSHelper[NodeKeyPair.GetAddress().ToHex().RemoveHexPrefix()];
            ConsensusDisposable =
                AElfDPoSObserver.SubscribeAElfDPoSMiningProcess(blockProducerInfoOfCurrentRound,
                    DPoSHelper.ExtraBlockTimeslot);

            //Update current round number.
            ConsensusMemory = DPoSHelper.CurrentRoundNumber.Value;
        }

        public async Task RecoverMining()
        {
            AElfDPoSObserver.RecoverMining();
            await Task.CompletedTask;
        }

        private async Task BroadcastTransaction(ITransaction tx)
        {
            if(tx.From.Equals(NodeKeyPair.GetAddress()))
                _logger?.Trace("Try to insert DPoS transaction to pool: " + tx.GetHash().ToHex() + ", threadId: " +
                               Thread.CurrentThread.ManagedThreadId);
            try
            {
                await _txPoolService.AddTxAsync(tx);
            }
            catch (Exception e)
            {
                _logger?.Trace("Transaction insertion failed: {0},\n{1}", e.Message, tx.GetTransactionInfo());
            }
        } 
    }
}