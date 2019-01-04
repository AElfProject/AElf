using System;
using System.Collections.Generic;
using System.Linq;
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
        private MockSetup _mock;

        private readonly SimpleExecutingService _executingService;
        
        public TransactionContext TransactionContext { get; private set; }

        public Address Sender => Address.Zero;

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
                Params = ByteString.CopyFrom(ParamsPacker.Pack(1, _mock.GetContractCode(_mock.ConsensusContractName)))
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
                Params = ByteString.CopyFrom(ParamsPacker.Pack(1, _mock.GetContractCode(_mock.TokenContractName)))
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
                Params = ByteString.CopyFrom(ParamsPacker.Pack(1, _mock.GetContractCode(_mock.DividendsContractName)))
            });
        }

        #region Consensus.Process

        #region View

        public Round GetRoundInfo(ulong roundNumber)
        {
            var tx = new Transaction
            {
                From = Sender,
                To = ConsensusContractAddress,
                IncrementId = MockSetup.NewIncrementId,
                MethodName = "GetRoundInfo",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(roundNumber))
            };

            ExecuteTransaction(tx);

            return TransactionContext.Trace.RetVal?.Data.DeserializeToPbMessage<Round>();
        }

        public ulong GetCurrentRoundNumber()
        {
            var tx = new Transaction
            {
                From = Sender,
                To = ConsensusContractAddress,
                IncrementId = MockSetup.NewIncrementId,
                MethodName = "GetCurrentRoundNumber",
                Params = ByteString.CopyFrom(ParamsPacker.Pack())
            };

            ExecuteTransaction(tx);

            var result = TransactionContext.Trace.RetVal?.Data.DeserializeToUInt64();
            return result ?? 0;
        }

        public ulong GetCurrentTermNumber()
        {
            var tx = new Transaction
            {
                From = Sender,
                To = ConsensusContractAddress,
                IncrementId = MockSetup.NewIncrementId,
                MethodName = "GetCurrentTermNumber",
                Params = ByteString.CopyFrom(ParamsPacker.Pack())
            };

            ExecuteTransaction(tx);

            var result = TransactionContext.Trace.RetVal?.Data.DeserializeToUInt64();
            return result ?? 0;
        }

        #endregion View

        #region Actions

        public void InitialTerm(ECKeyPair minerKeyPair, Term initialTerm)
        {
            var tx = new Transaction
            {
                From = GetAddress(minerKeyPair),
                To = ConsensusContractAddress,
                IncrementId = MockSetup.NewIncrementId,
                MethodName = "InitialTerm",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(initialTerm, 1))
            };
            var signer = new ECSigner();
            var signature = signer.Sign(minerKeyPair, tx.GetHash().DumpByteArray());
            tx.Sigs.Add(ByteString.CopyFrom(signature.SigBytes));

            ExecuteTransaction(tx);
        }

        public void NextTerm(ECKeyPair minerKeyPair, Term nextTerm)
        {
            var tx = new Transaction
            {
                From = GetAddress(minerKeyPair),
                To = ConsensusContractAddress,
                IncrementId = MockSetup.NewIncrementId,
                MethodName = "NextTerm",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(nextTerm))
            };
            var signer = new ECSigner();
            var signature = signer.Sign(minerKeyPair, tx.GetHash().DumpByteArray());
            tx.Sigs.Add(ByteString.CopyFrom(signature.SigBytes));

            ExecuteTransaction(tx);
        }

        public void PackageOutValue(ECKeyPair minerKeyPair, ToPackage toPackage)
        {
            var tx = new Transaction
            {
                From = GetAddress(minerKeyPair),
                To = ConsensusContractAddress,
                IncrementId = MockSetup.NewIncrementId,
                MethodName = "PackageOutValue",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(toPackage))
            };
            var signer = new ECSigner();
            var signature = signer.Sign(minerKeyPair, tx.GetHash().DumpByteArray());
            tx.Sigs.Add(ByteString.CopyFrom(signature.SigBytes));

            ExecuteTransaction(tx);
        }

        public void BroadcastInValue(ECKeyPair minerKeyPair, ToBroadcast toBroadcast)
        {
            var tx = new Transaction
            {
                From = GetAddress(minerKeyPair),
                To = ConsensusContractAddress,
                IncrementId = MockSetup.NewIncrementId,
                MethodName = "BroadcastInValue",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(toBroadcast))
            };
            var signer = new ECSigner();
            var signature = signer.Sign(minerKeyPair, tx.GetHash().DumpByteArray());
            tx.Sigs.Add(ByteString.CopyFrom(signature.SigBytes));
            
            ExecuteTransaction(tx);
        }

        public void NextRound(ECKeyPair minerKeyPair, Forwarding forwarding)
        {
            var tx = new Transaction
            {
                From = GetAddress(minerKeyPair),
                To = ConsensusContractAddress,
                IncrementId = MockSetup.NewIncrementId,
                MethodName = "NextRound",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(forwarding))
            };
            var signer = new ECSigner();
            var signature = signer.Sign(minerKeyPair, tx.GetHash().DumpByteArray());
            tx.Sigs.Add(ByteString.CopyFrom(signature.SigBytes));

            ExecuteTransaction(tx);
        }
        
        public void InitialBalance(ECKeyPair minerKeyPair, Address address, ulong amount)
        {
            var tx = new Transaction
            {
                From = GetAddress(minerKeyPair),
                To = ConsensusContractAddress,
                IncrementId = MockSetup.NewIncrementId,
                MethodName = "InitialBalance",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(address, amount))
            };
            var signer = new ECSigner();
            var signature = signer.Sign(minerKeyPair, tx.GetHash().DumpByteArray());
            tx.Sigs.Add(ByteString.CopyFrom(signature.SigBytes));

            ExecuteTransaction(tx);
        }

        #endregion Actions

        #endregion

        #region Election

        #region View Only Methods

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

        #endregion View Only Methods

        #region Actions

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

        #endregion Actions

        #endregion Election

        #region ABI (Public) Methods

        #region View Only Methods

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

        public string TokenName()
        {
            var tx = new Transaction
            {
                From = Sender,
                To = TokenContractAddress,
                IncrementId = MockSetup.NewIncrementId,
                MethodName = "TokenName",
                Params = ByteString.CopyFrom(ParamsPacker.Pack())
            };

            ExecuteTransaction(tx);

            TransactionContext.Trace.SmartCommitChangesAsync(_mock.StateManager).Wait();
            return TransactionContext.Trace.RetVal?.Data.DeserializeToString();
        }

        public ulong TotalSupply()
        {
            var tx = new Transaction
            {
                From = Sender,
                To = TokenContractAddress,
                IncrementId = MockSetup.NewIncrementId,
                MethodName = "TotalSupply",
                Params = ByteString.CopyFrom(ParamsPacker.Pack())
            };

            ExecuteTransaction(tx);

            TransactionContext.Trace.SmartCommitChangesAsync(_mock.StateManager).Wait();
            return TransactionContext.Trace.RetVal?.Data.DeserializeToUInt64() ?? 0;
        }

        public uint Decimals()
        {
            var tx = new Transaction
            {
                From = Sender,
                To = TokenContractAddress,
                IncrementId = MockSetup.NewIncrementId,
                MethodName = "Decimals",
                Params = ByteString.CopyFrom(ParamsPacker.Pack())
            };

            ExecuteTransaction(tx);

            TransactionContext.Trace.SmartCommitChangesAsync(_mock.StateManager).Wait();
            return TransactionContext.Trace.RetVal?.Data.DeserializeToUInt32() ?? 0;
        }

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

        public ulong Allowance(Address owner, Address spender)
        {
            var tx = new Transaction
            {
                From = Sender,
                To = TokenContractAddress,
                IncrementId = MockSetup.NewIncrementId,
                MethodName = "Allowance",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(owner, spender))
            };

            ExecuteTransaction(tx);

            TransactionContext.Trace.SmartCommitChangesAsync(_mock.StateManager).Wait();
            return TransactionContext.Trace.RetVal?.Data.DeserializeToUInt64() ?? 0;
        }

        #endregion View Only Methods

        #region Actions

        public void Initialize(string symbol, string tokenName, ulong totalSupply, uint decimals)
        {
            var tx = new Transaction
            {
                From = Sender,
                To = TokenContractAddress,
                IncrementId = MockSetup.NewIncrementId,
                MethodName = "Initialize",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(symbol, tokenName, totalSupply, decimals))
            };
            
            ExecuteTransaction(tx);
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

        public void TransferFrom(Address from, Address to, ulong amount)
        {
            var tx = new Transaction
            {
                From = from,
                To = TokenContractAddress,
                IncrementId = MockSetup.NewIncrementId,
                MethodName = "Transfer",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(to, amount))
            };

            ExecuteTransaction(tx);
        }

        public void Approve(Address spender, ulong amount)
        {
            var tx = new Transaction
            {
                From = Sender,
                To = TokenContractAddress,
                IncrementId = MockSetup.NewIncrementId,
                MethodName = "Approve",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(spender, amount))
            };

            ExecuteTransaction(tx);
        }

        public void UnApprove(Address spender, ulong amount)
        {
            var tx = new Transaction
            {
                From = Sender,
                To = TokenContractAddress,
                IncrementId = MockSetup.NewIncrementId,
                MethodName = "UnApprove",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(spender, amount))
            };

            ExecuteTransaction(tx);
        }

        public Address GetContractOwner(Address scZeroAddress)
        {
            var executive = _mock.GetExecutiveAsync(scZeroAddress).Result;

            var tx = new Transaction
            {
                From = Sender,
                To = scZeroAddress,
                IncrementId = MockSetup.NewIncrementId,
                MethodName = "GetContractOwner",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(TokenContractAddress))
            };

            ExecuteTransaction(tx);

            return TransactionContext.Trace.RetVal?.Data.DeserializeToPbMessage<Address>();
        }

        #endregion Actions

        #endregion ABI (Public) Methods

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
    }
}