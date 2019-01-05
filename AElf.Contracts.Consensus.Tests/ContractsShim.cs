using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.SmartContract;
using AElf.Kernel;
using AElf.Types.CSharp;
using ByteString = Google.Protobuf.ByteString;
using AElf.Common;
using AElf.Cryptography.ECDSA;
using AElf.Execution.Execution;
using Google.Protobuf.WellKnownTypes;

// ReSharper disable once CheckNamespace
namespace AElf.Contracts.Consensus.Tests
{
    public class ContractsShim
    {
        private readonly MockSetup _mock;

        private readonly SimpleExecutingService _executingService;

        public TransactionContext TransactionContext { get; private set; }

        public Address Sender => Address.Zero;

        public ECKeyPair SenderKeyPair => new KeyPairGenerator().Generate();

        public Address ConsensusContractAddress { get; set; }
        public Address TokenContractAddress { get; set; }
        public Address DividendsContractAddress { get; set; }

        public ContractsShim(MockSetup mock, SimpleExecutingService executingService)
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
                Params = ByteString.CopyFrom(ParamsPacker.Pack(1, MockSetup.GetContractCode(_mock.ConsensusContractName)))
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
                Params = ByteString.CopyFrom(ParamsPacker.Pack(1, MockSetup.GetContractCode(_mock.DividendsContractName)))
            });
        }

        #region Consensus.Query

        public Round GetRoundInfo(ulong roundNumber)
        {
            ExecuteAction(ConsensusContractAddress, nameof(GetRoundInfo), SenderKeyPair);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToPbMessage<Round>();
        }

        public ulong GetCurrentRoundNumber()
        {
            ExecuteAction(ConsensusContractAddress, nameof(GetCurrentRoundNumber), SenderKeyPair);
            var result = TransactionContext.Trace.RetVal?.Data.DeserializeToUInt64();
            return result ?? 0;
        }

        public ulong GetCurrentTermNumber()
        {
            ExecuteAction(ConsensusContractAddress, nameof(GetCurrentTermNumber), SenderKeyPair);

            var result = TransactionContext.Trace.RetVal?.Data.DeserializeToUInt64();
            return result ?? 0;
        }
        
        public bool? IsCandidate(string publicKey)
        {
            var tx = new Transaction
            {
                From = Sender,
                To = ConsensusContractAddress,
                IncrementId = MockSetup.NewIncrementId,
                MethodName = "IsCandidate",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(publicKey))
            };

            ExecuteTransaction(tx);

            TransactionContext.Trace.SmartCommitChangesAsync(_mock.StateManager).Wait();
            return TransactionContext.Trace.RetVal?.Data.DeserializeToBool();
        }

        public string GetCandidatesListToFriendlyString()
        {
            var tx = new Transaction
            {
                From = Sender,
                To = ConsensusContractAddress,
                IncrementId = MockSetup.NewIncrementId,
                MethodName = "GetCandidatesListToFriendlyString",
                Params = ByteString.CopyFrom(ParamsPacker.Pack())
            };

            ExecuteTransaction(tx);

            TransactionContext.Trace.SmartCommitChangesAsync(_mock.StateManager).Wait();
            return TransactionContext.Trace.RetVal?.Data.DeserializeToString();
        }

        public Tickets GetTicketsInfo(ECKeyPair keyPair)
        {
            var tx = new Transaction
            {
                From = Sender,
                To = ConsensusContractAddress,
                IncrementId = MockSetup.NewIncrementId,
                MethodName = "GetTicketsInfo",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(keyPair.PublicKey.ToHex()))
            };
            var signer = new ECSigner();
            var signature = signer.Sign(keyPair, tx.GetHash().DumpByteArray());
            tx.Sigs.Add(ByteString.CopyFrom(signature.SigBytes));

            ExecuteTransaction(tx);

            TransactionContext.Trace.SmartCommitChangesAsync(_mock.StateManager).Wait();
            return TransactionContext.Trace.RetVal?.Data.DeserializeToPbMessage<Tickets>();
        }

        public string GetTicketsInfoToFriendlyString(ECKeyPair keyPair)
        {
            var tx = new Transaction
            {
                From = Sender,
                To = ConsensusContractAddress,
                IncrementId = MockSetup.NewIncrementId,
                MethodName = "GetTicketsInfoToFriendlyString",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(keyPair.PublicKey.ToHex()))
            };
            var signer = new ECSigner();
            var signature = signer.Sign(keyPair, tx.GetHash().DumpByteArray());
            tx.Sigs.Add(ByteString.CopyFrom(signature.SigBytes));

            ExecuteTransaction(tx);

            TransactionContext.Trace.SmartCommitChangesAsync(_mock.StateManager).Wait();
            return TransactionContext.Trace.RetVal?.Data.DeserializeToString();
        }

        public StringList GetCurrentVictories()
        {
            var tx = new Transaction
            {
                From = Sender,
                To = ConsensusContractAddress,
                IncrementId = MockSetup.NewIncrementId,
                MethodName = "GetCurrentVictories",
                Params = ByteString.CopyFrom(ParamsPacker.Pack())
            };

            ExecuteTransaction(tx);

            TransactionContext.Trace.SmartCommitChangesAsync(_mock.StateManager).Wait();
            return TransactionContext.Trace.RetVal?.Data.DeserializeToPbMessage<StringList>();
        }

        public TermSnapshot GetTermSnapshot(ulong termNumber)
        {
            var tx = new Transaction
            {
                From = Sender,
                To = ConsensusContractAddress,
                IncrementId = MockSetup.NewIncrementId,
                MethodName = "GetTermSnapshot",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(termNumber))
            };

            ExecuteTransaction(tx);

            TransactionContext.Trace.SmartCommitChangesAsync(_mock.StateManager).Wait();
            return TransactionContext.Trace.RetVal?.Data.DeserializeToPbMessage<TermSnapshot>();
        }

        #endregion

        #region Consensus.Process

        public void InitialTerm(ECKeyPair minerKeyPair, Term initialTerm)
        {
            ExecuteAction(ConsensusContractAddress, nameof(InitialTerm), minerKeyPair, initialTerm);
        }

        public void NextTerm(ECKeyPair minerKeyPair, Term nextTerm)
        {
            ExecuteAction(ConsensusContractAddress, nameof(NextTerm), minerKeyPair, nextTerm);
        }

        public void PackageOutValue(ECKeyPair minerKeyPair, ToPackage toPackage)
        {
            ExecuteAction(ConsensusContractAddress, nameof(PackageOutValue), minerKeyPair, toPackage);
        }

        public void BroadcastInValue(ECKeyPair minerKeyPair, ToBroadcast toBroadcast)
        {
            ExecuteAction(ConsensusContractAddress, nameof(BroadcastInValue), minerKeyPair, toBroadcast);
        }

        public void NextRound(ECKeyPair minerKeyPair, Forwarding forwarding)
        {
            ExecuteAction(ConsensusContractAddress, nameof(NextRound), minerKeyPair, forwarding);
        }

        public void InitialBalance(ECKeyPair minerKeyPair, Address address, ulong amount)
        {
            ExecuteAction(ConsensusContractAddress, nameof(InitialBalance), minerKeyPair, address, amount);
        }

        #endregion

        #region Consensus.Election
        
        public void AnnounceElection(ECKeyPair candidateKeyPair, string alias = "")
        {
            var tx = new Transaction
            {
                From = GetAddress(candidateKeyPair),
                To = ConsensusContractAddress,
                IncrementId = MockSetup.NewIncrementId,
                MethodName = "AnnounceElection",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(alias))
            };
            var signer = new ECSigner();
            var signature = signer.Sign(candidateKeyPair, tx.GetHash().DumpByteArray());
            tx.Sigs.Add(ByteString.CopyFrom(signature.SigBytes));

            ExecuteTransaction(tx);
        }

        public void QuitElection(ECKeyPair candidateKeyPair)
        {
            var tx = new Transaction
            {
                From = GetAddress(candidateKeyPair),
                To = ConsensusContractAddress,
                IncrementId = MockSetup.NewIncrementId,
                MethodName = "QuitElection",
                Params = ByteString.CopyFrom(ParamsPacker.Pack())
            };
            var signer = new ECSigner();
            var signature = signer.Sign(candidateKeyPair, tx.GetHash().DumpByteArray());
            tx.Sigs.Add(ByteString.CopyFrom(signature.SigBytes));

            ExecuteTransaction(tx);
        }

        public void Vote(ECKeyPair voterKeyPair, ECKeyPair candidateKeyPair, ulong amount, int lockDays)
        {
            var tx = new Transaction
            {
                From = GetAddress(voterKeyPair),
                To = ConsensusContractAddress,
                IncrementId = MockSetup.NewIncrementId,
                MethodName = "Vote",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(candidateKeyPair.PublicKey.ToHex(), amount, lockDays,
                    DateTime.UtcNow.ToTimestamp()))
            };
            var signer = new ECSigner();
            var signature = signer.Sign(voterKeyPair, tx.GetHash().DumpByteArray());
            tx.Sigs.Add(ByteString.CopyFrom(signature.SigBytes));

            ExecuteTransaction(tx);
        }

        public void ReceiveAllDividends(ECKeyPair ownerKeyPair)
        {
            var tx = new Transaction
            {
                From = GetAddress(ownerKeyPair),
                To = ConsensusContractAddress,
                IncrementId = MockSetup.NewIncrementId,
                MethodName = "ReceiveAllDividends",
                Params = ByteString.CopyFrom(ParamsPacker.Pack())
            };
            var signer = new ECSigner();
            var signature = signer.Sign(ownerKeyPair, tx.GetHash().DumpByteArray());
            tx.Sigs.Add(ByteString.CopyFrom(signature.SigBytes));

            ExecuteTransaction(tx);
        }
        #endregion Consensus.Election

        #region Dividends

        public ulong GetTermDividends(ulong termNumber)
        {
            var tx = new Transaction
            {
                From = Sender,
                To = DividendsContractAddress,
                IncrementId = MockSetup.NewIncrementId,
                MethodName = "GetTermDividends",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(termNumber))
            };

            ExecuteTransaction(tx);

            TransactionContext.Trace.SmartCommitChangesAsync(_mock.StateManager).Wait();
            return TransactionContext.Trace.RetVal?.Data.DeserializeToUInt64() ?? 0;
        }

        public ulong CheckStandardDividendsOfPreviousTerm()
        {
            var tx = new Transaction
            {
                From = Sender,
                To = DividendsContractAddress,
                IncrementId = MockSetup.NewIncrementId,
                MethodName = "CheckStandardDividendsOfPreviousTerm",
                Params = ByteString.CopyFrom(ParamsPacker.Pack())
            };

            ExecuteTransaction(tx);

            TransactionContext.Trace.SmartCommitChangesAsync(_mock.StateManager).Wait();
            return TransactionContext.Trace.RetVal?.Data.DeserializeToUInt64() ?? 0;
        }

        #endregion

        #region Token

        public ulong BalanceOf(Address owner)
        {
            var tx = new Transaction
            {
                From = Sender,
                To = TokenContractAddress,
                IncrementId = MockSetup.NewIncrementId,
                MethodName = "BalanceOf",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(owner))
            };

            ExecuteTransaction(tx);

            TransactionContext.Trace.SmartCommitChangesAsync(_mock.StateManager).Wait();
            return TransactionContext.Trace.RetVal?.Data.DeserializeToUInt64() ?? 0;
        }
        
        public void Transfer(Address to, ulong amount)
        {
            var tx = new Transaction
            {
                From = Sender,
                To = TokenContractAddress,
                IncrementId = MockSetup.NewIncrementId,
                MethodName = "Transfer",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(to, amount))
            };

            ExecuteTransaction(tx);
        }

        #endregion

        #region Private methods

        private Address GetAddress(ECKeyPair keyPair)
        {
            return Address.FromPublicKey(keyPair.PublicKey);
        }

        private void ExecuteTransaction(Transaction tx)
        {
            var traces = _executingService.ExecuteAsync(new List<Transaction> {tx},
                Hash.FromString(GlobalConfig.DefaultChainId), DateTime.UtcNow, new CancellationToken(), null,
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
        
        private void ExecuteAction(Address contractAddress, string methodName, ECKeyPair callerKeyPair, params object[] objects)
        {
            var tx = new Transaction
            {
                From = GetAddress(callerKeyPair),
                To = contractAddress,
                IncrementId = MockSetup.NewIncrementId,
                MethodName = methodName,
                Params = ByteString.CopyFrom(ParamsPacker.Pack(objects))
            };
            
            var signer = new ECSigner();
            var signature = signer.Sign(callerKeyPair, tx.GetHash().DumpByteArray());
            tx.Sigs.Add(ByteString.CopyFrom(signature.SigBytes));
            
            ExecuteTransaction(tx);
        }

        #endregion
    }
}