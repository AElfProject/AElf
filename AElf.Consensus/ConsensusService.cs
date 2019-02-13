using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Execution.Execution;
using AElf.Kernel;
using AElf.Kernel.Managers;
using AElf.Kernel.Types;
using AElf.SmartContract;
using AElf.Types.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.EventBus;
using Volo.Abp.EventBus.Distributed;

namespace AElf.Consensus
{
    public class ConsensusService : IConsensusService
    {
        private readonly IConsensusObserver _consensusObserver;
        
        private readonly IExecutingService _executingService;
        private readonly StateManager _stateManager;

        public IEventBus EventBus { get; set; }

        private IDisposable _consensusObservables = null;

        public ConsensusService(IConsensusObserver consensusObserver, IExecutingService executingService, StateManager stateManager)
        {
            _consensusObserver = consensusObserver;
            _executingService = executingService;
            _stateManager = stateManager;
            
            EventBus = NullDistributedEventBus.Instance;
        }

        public bool ValidateConsensus(int chainId, Address fromAddress, byte[] consensusInformation)
        {
            return ExecuteConsensusContract(chainId, fromAddress, ConsensusMethod.ValidateConsensus, null)
                .DeserializeToPbMessage<ValidationResult>().Success;
        }

        public int GetCountingMilliseconds(int chainId, Address fromAddress)
        {
            return ExecuteConsensusContract(chainId, fromAddress, ConsensusMethod.GetCountingMilliseconds,
                Timestamp.FromDateTime(DateTime.UtcNow)).DeserializeToInt32();
        }

        public byte[] GetNewConsensusInformation(int chainId, Address fromAddress)
        {
            return ExecuteConsensusContract(chainId, fromAddress, ConsensusMethod.GetNewConsensusInformation, null)
                .DeserializeToBytes();
        }

        public TransactionList GenerateConsensusTransactions(int chainId, Address fromAddress, ulong currentBlockHeight, Hash previousBlockHash)
        {
            return ExecuteConsensusContract(chainId, fromAddress, ConsensusMethod.GenerateConsensusTransactions,
                currentBlockHeight, previousBlockHash, null).DeserializeToPbMessage<TransactionList>();
        }

        private ByteString ExecuteConsensusContract(int chainId, Address fromAddress, ConsensusMethod consensusMethod,
            params object[] objects)
        {
            var tx = new Transaction
            {
                From = fromAddress,
                To = ContractHelpers.GetConsensusContractAddress(chainId),
                MethodName = consensusMethod.ToString(),
                Params = ByteString.CopyFrom(ParamsPacker.Pack(objects))
            };

            var traces = _executingService.ExecuteAsync(new List<Transaction> {tx},
                chainId, DateTime.UtcNow, new CancellationToken(), null,
                TransactionType.ContractTransaction, true).Result;
            CommitChangesAsync(traces.Last()).Wait();
            return traces.Last().RetVal?.Data;
        }

        private async Task CommitChangesAsync(TransactionTrace trace)
        {
            await trace.SmartCommitChangesAsync(_stateManager);
        }

        enum ConsensusMethod
        {
            ValidateConsensus,
            GetCountingMilliseconds,
            GetNewConsensusInformation,
            GenerateConsensusTransactions
        }
    }
}