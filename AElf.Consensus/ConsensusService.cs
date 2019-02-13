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
using AElf.SmartContract;
using AElf.Types.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.EventBus;
using Volo.Abp.EventBus.Local;

namespace AElf.Consensus
{
    public class ConsensusService : IConsensusService
    {
        private readonly IConsensusObserver _consensusObserver;
        
        private IExecutingService _executingService;
        private StateManager _stateManager;

        public IEventBus EventBus { get; set; }

        private IDisposable _consensusObservables = null;

        public ConsensusService(IConsensusObserver consensusObserver, IExecutingService executingService, StateManager stateManager)
        {
            _consensusObserver = consensusObserver;
            _executingService = executingService;
            _stateManager = stateManager;
            
            EventBus = NullLocalEventBus.Instance;
        }

        public ValidationResult ValidateConsensus(byte[] consensusInformation)
        {
            throw new NotImplementedException();
        }

        public int GetCountingMilliseconds(Timestamp timestamp)
        {
            throw new NotImplementedException();
        }

        public IMessage GetNewConsensusInformation()
        {
            throw new NotImplementedException();
        }

        public TransactionList GenerateConsensusTransactions(ulong currentBlockHeight, Hash previousBlockHash)
        {
            throw new NotImplementedException();
        }
        
        public ByteString ExecuteTransaction(Transaction tx)
        {
            if (tx == null)
            {
                return null;
            }

            var traces = _executingService.ExecuteAsync(new List<Transaction> {tx},
                Hash.FromString(GlobalConfig.DefaultChainId), DateTime.UtcNow, new CancellationToken(), null,
                TransactionType.ContractTransaction, true).Result;
            CommitChangesAsync(traces.Last()).Wait();
            return traces.Last().RetVal?.Data;
        }

        public void ExecuteAction(Address contractAddress, string methodName, ECKeyPair callerKeyPair,
            params object[] objects)
        {
            var tx = new Transaction
            {
                From = GetAddress(callerKeyPair),
                To = contractAddress,
                MethodName = methodName,
                Params = ByteString.CopyFrom(ParamsPacker.Pack(objects))
            };

            var signature = CryptoHelpers.SignWithPrivateKey(callerKeyPair.PrivateKey, tx.GetHash().DumpByteArray());
            tx.Sigs.Add(ByteString.CopyFrom(signature));

            ExecuteTransaction(tx);
        }
        
        private async Task CommitChangesAsync(TransactionTrace trace)
        {
            await trace.SmartCommitChangesAsync(_stateManager);
        }
        
        private Address GetAddress(ECKeyPair keyPair)
        {
            return Address.FromPublicKey(keyPair.PublicKey);
        }
    }
}