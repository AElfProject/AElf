using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.SmartContract;
using AElf.Kernel;
using AElf.Types.CSharp;
using ByteString = Google.Protobuf.ByteString;
using AElf.Common;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Execution.Execution;
using AElf.Kernel.Consensus;
using AElf.Kernel.Types;
using Org.BouncyCastle.Crypto.Generators;
using Volo.Abp.DependencyInjection;

namespace AElf.Contracts.Consensus.DPoS.Tests
{
    // ReSharper disable UnusedMember.Global
    public class ContractsShim : ITransientDependency
    {
        private readonly MockSetup _mock;

        private readonly IExecutingService _executingService;

        public TransactionContext TransactionContext { get; private set; }

        private static Address Sender => Address.Zero;

        // To query something.
        private static ECKeyPair SenderKeyPair => CryptoHelpers.GenerateKeyPair();

        public Address ConsensusContractAddress { get; }
        private Address TokenContractAddress { get; }
        public Address DividendsContractAddress { get; }

        public ContractsShim(MockSetup mock, IExecutingService executingService)
        {
            _mock = mock;
            _executingService = executingService;

            DeployConsensusContractAsync();
            DeployTokenContractAsync();
            DeployDividendsContractAsync();

            ConsensusContractAddress = ContractHelpers.GetConsensusContractAddress(_mock.ChainId);
            TokenContractAddress = ContractHelpers.GetTokenContractAddress(_mock.ChainId);
            DividendsContractAddress = ContractHelpers.GetDividendsContractAddress(_mock.ChainId);
        }

        #region Private methods

        private async Task CommitChangesAsync(TransactionTrace trace)
        {
            await trace.SmartCommitChangesAsync(_mock.StateManager);
        }

        private void DeployConsensusContractAsync()
        {
            ExecuteTransaction(new Transaction
            {
                From = Sender,
                To = ContractHelpers.GetGenesisBasicContractAddress(_mock.ChainId),
                IncrementId = 0,
                MethodName = "DeploySmartContract",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(1,
                    MockSetup.GetContractCode(_mock.ConsensusContractName)))
            });
        }

        private void DeployTokenContractAsync()
        {
            ExecuteTransaction(new Transaction
            {
                From = Sender,
                To = ContractHelpers.GetGenesisBasicContractAddress(_mock.ChainId),
                IncrementId = 0,
                MethodName = "DeploySmartContract",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(1, MockSetup.GetContractCode(_mock.TokenContractName)))
            });
        }

        private void DeployDividendsContractAsync()
        {
            ExecuteTransaction(new Transaction
            {
                From = Sender,
                To = ContractHelpers.GetGenesisBasicContractAddress(_mock.ChainId),
                IncrementId = 0,
                MethodName = "DeploySmartContract",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(1,
                    MockSetup.GetContractCode(_mock.DividendsContractName)))
            });
        }

        private Address GetAddress(ECKeyPair keyPair)
        {
            return Address.FromPublicKey(keyPair.PublicKey);
        }

        public void ExecuteTransaction(Transaction tx, ECKeyPair callerKeyPair = null)
        {
            if (tx == null)
            {
                return;
            }

            if (callerKeyPair != null)
            {
                var signature = CryptoHelpers.SignWithPrivateKey(callerKeyPair.PrivateKey, tx.GetHash().DumpByteArray());
                tx.Sigs.Add(ByteString.CopyFrom(signature));
            }
            
            var traces = _executingService.ExecuteAsync(new List<Transaction> {tx},
                GlobalConfig.DefaultChainId.ConvertBase58ToChainId(), DateTime.UtcNow, new CancellationToken(), null,
                TransactionType.ContractTransaction, true).Result;
            foreach (var transactionTrace in traces)
            {
                CommitChangesAsync(transactionTrace).Wait();

                TransactionContext = new TransactionContext
                {
                    Trace = transactionTrace
                };
            }
        }

        public void ExecuteAction(Address contractAddress, string methodName, ECKeyPair callerKeyPair,
            params object[] objects)
        {
            var tx = new Transaction
            {
                From = GetAddress(callerKeyPair),
                To = contractAddress,
                IncrementId = MockSetup.NewIncrementId,
                MethodName = methodName,
                Params = ByteString.CopyFrom(ParamsPacker.Pack(objects))
            };

            var signature = CryptoHelpers.SignWithPrivateKey(callerKeyPair.PrivateKey, tx.GetHash().DumpByteArray());
            tx.Sigs.Add(ByteString.CopyFrom(signature));

            ExecuteTransaction(tx);
        }

        #endregion
    }
}