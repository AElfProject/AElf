using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.SmartContract;
using AElf.Kernel;
using AElf.Types.CSharp;
using ByteString = Google.Protobuf.ByteString;
using AElf.Common;
using AElf.Cryptography.ECDSA;

namespace AElf.Contracts.Consensus.Tests
{
    public class ConsensusContractShim
    {
        private MockSetup _mock;
        public IExecutive ExecutiveForConsensus { get; set; }
        public IExecutive ExecutiveForToken { get; set; }
        public IExecutive ExecutiveForDividends { get; set; }

        public TransactionContext TransactionContext { get; private set; }

        public Address Sender
        {
            get => Address.Zero;
        }

        public Address ConsensusContractAddress { get; set; }
        public Address TokenContractAddress { get; set; }
        public Address DividendsContractAddress { get; set; }

        public ConsensusContractShim(MockSetup mock)
        {
            _mock = mock;
            Init();
        }

        private void Init()
        {
            DeployConsensusContractAsync().Wait();
            DeployTokenContractAsync().Wait();
            DeployDividendsContractAsync().Wait();
            
            var task1 = _mock.GetExecutiveAsync(ConsensusContractAddress);
            task1.Wait();
            ExecutiveForConsensus = task1.Result;
            
            var task2 = _mock.GetExecutiveAsync(TokenContractAddress);
            task2.Wait();
            ExecutiveForToken = task2.Result;
            
            var task3 = _mock.GetExecutiveAsync(DividendsContractAddress);
            task3.Wait();
            ExecutiveForDividends = task3.Result;
        }

        private async Task<TransactionContext> PrepareTransactionContextAsync(Transaction tx)
        {
            var chainContext = await _mock.ChainContextService.GetChainContextAsync(_mock.ChainId);
            var tc = new TransactionContext
            {
                PreviousBlockHash = chainContext.BlockHash,
                BlockHeight = chainContext.BlockHeight,
                Transaction = tx,
                Trace = new TransactionTrace()
            };
            return tc;
        }

        private TransactionContext PrepareTransactionContext(Transaction tx)
        {
            var task = PrepareTransactionContextAsync(tx);
            task.Wait();
            return task.Result;
        }

        private async Task CommitChangesAsync(TransactionTrace trace)
        {
            await trace.CommitChangesAsync(_mock.StateStore);
        }

        private async Task DeployConsensusContractAsync()
        {
            var address0 = ContractHelpers.GetGenesisBasicContractAddress(_mock.ChainId);
            var executive0 = await _mock.GetExecutiveAsync(address0);

            var tx = new Transaction
            {
                From = Sender,
                To = address0,
                IncrementId = 0,
                MethodName = "DeploySmartContract",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(1, _mock.GetContractCode(_mock.ConsensusContractName)))
            };

            var tc = await PrepareTransactionContextAsync(tx);
            await executive0.SetTransactionContext(tc).Apply();
            await CommitChangesAsync(tc.Trace);
            ConsensusContractAddress = ContractHelpers.GetConsensusContractAddress(_mock.ChainId);
        }
        
        private async Task DeployTokenContractAsync()
        {
            var address0 = ContractHelpers.GetGenesisBasicContractAddress(_mock.ChainId);
            var executive0 = await _mock.GetExecutiveAsync(address0);

            var tx = new Transaction
            {
                From = Sender,
                To = address0,
                IncrementId = 0,
                MethodName = "DeploySmartContract",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(1, _mock.GetContractCode(_mock.TokenContractName)))
            };

            var tc = await PrepareTransactionContextAsync(tx);
            await executive0.SetTransactionContext(tc).Apply();
            await CommitChangesAsync(tc.Trace);
            TokenContractAddress = ContractHelpers.GetTokenContractAddress(_mock.ChainId);
        }

        private async Task DeployDividendsContractAsync()
        {
            var address0 = ContractHelpers.GetGenesisBasicContractAddress(_mock.ChainId);
            var executive0 = await _mock.GetExecutiveAsync(address0);

            var tx = new Transaction
            {
                From = Sender,
                To = address0,
                IncrementId = 0,
                MethodName = "DeploySmartContract",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(1, _mock.GetContractCode(_mock.DividendsContractName)))
            };

            var tc = await PrepareTransactionContextAsync(tx);
            await executive0.SetTransactionContext(tc).Apply();
            await CommitChangesAsync(tc.Trace);
            DividendsContractAddress = ContractHelpers.GetDividendsContractAddress(_mock.ChainId);
        }
        
        #region Process

        #region View Only Methods
        
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
            
            TransactionContext = new TransactionContext
            {
                Transaction = tx
            };
            ExecutiveForConsensus.SetTransactionContext(TransactionContext).Apply().Wait();
            TransactionContext.Trace.CommitChangesAsync(_mock.StateStore).Wait();
            return TransactionContext.Trace.RetVal?.Data.DeserializeToPbMessage<Round>();
        }

        #endregion View Only Methods

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

            TransactionContext = PrepareTransactionContext(tx);
            ExecutiveForConsensus.SetTransactionContext(TransactionContext).Apply().Wait();
            CommitChangesAsync(TransactionContext.Trace).Wait();
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

            TransactionContext = PrepareTransactionContext(tx);
            
            ExecutiveForConsensus.SetTransactionContext(TransactionContext).Apply().Wait();

            var tc = PrepareTransactionContext(
                TransactionContext.Trace.InlineTransactions.FirstOrDefault(t => t.MethodName == "AddDividends"));
            if (tc != null)
            {
                ExecutiveForDividends.SetTransactionContext(tc).Apply().Wait();
                TransactionContext.Trace.InlineTraces.Add(tc.Trace);
            }

            foreach (var transaction in TransactionContext.Trace.InlineTransactions.Where(t => t.MethodName == "Transfer"))
            {
                var tcOfTransfer = PrepareTransactionContext(transaction);
                ExecutiveForToken.SetTransactionContext(tcOfTransfer).Apply().Wait();
                TransactionContext.Trace.InlineTraces.Add(tcOfTransfer.Trace);
            }
            
            CommitChangesAsync(TransactionContext.Trace).Wait();
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

            TransactionContext = PrepareTransactionContext(tx);
            ExecutiveForConsensus.SetTransactionContext(TransactionContext).Apply().Wait();
            CommitChangesAsync(TransactionContext.Trace).Wait();
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

            TransactionContext = PrepareTransactionContext(tx);
            ExecutiveForConsensus.SetTransactionContext(TransactionContext).Apply().Wait();
            CommitChangesAsync(TransactionContext.Trace).Wait();
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

            TransactionContext = new TransactionContext
            {
                Transaction = tx
            };
            ExecutiveForConsensus.SetTransactionContext(TransactionContext).Apply().Wait();
            TransactionContext.Trace.CommitChangesAsync(_mock.StateStore).Wait();
            return TransactionContext.Trace.RetVal?.Data.DeserializeToBool();
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

            TransactionContext = new TransactionContext
            {
                Transaction = tx
            };

            ExecutiveForConsensus.SetTransactionContext(TransactionContext).Apply().Wait();
            TransactionContext.Trace.CommitChangesAsync(_mock.StateStore).Wait();
            return TransactionContext.Trace.RetVal?.Data.DeserializeToPbMessage<Tickets>();
        }

        public string GetCurrentVictories()
        {
            var tx = new Transaction
            {
                From = Sender,
                To = ConsensusContractAddress,
                IncrementId = MockSetup.NewIncrementId,
                MethodName = "GetCurrentVictories",
                Params = ByteString.CopyFrom(ParamsPacker.Pack())
            };

            TransactionContext = new TransactionContext
            {
                Transaction = tx
            };
            ExecutiveForConsensus.SetTransactionContext(TransactionContext).Apply().Wait();
            TransactionContext.Trace.CommitChangesAsync(_mock.StateStore).Wait();
            return TransactionContext.Trace.RetVal?.Data.DeserializeToString();
        }

        #endregion View Only Methods

        #region Actions

        public void AnnounceElection(ECKeyPair candidateKeyPair)
        {
            var tx = new Transaction
            {
                From = GetAddress(candidateKeyPair),
                To = ConsensusContractAddress,
                IncrementId = MockSetup.NewIncrementId,
                MethodName = "AnnounceElection",
                Params = ByteString.CopyFrom(ParamsPacker.Pack())
            };
            var signer = new ECSigner();
            var signature = signer.Sign(candidateKeyPair, tx.GetHash().DumpByteArray());
            tx.Sigs.Add(ByteString.CopyFrom(signature.SigBytes));

            TransactionContext = PrepareTransactionContext(tx);
            ExecutiveForConsensus.SetTransactionContext(TransactionContext).Apply().Wait();
            var tc = PrepareTransactionContext(TransactionContext.Trace.InlineTransactions[0]);
            ExecutiveForToken.SetTransactionContext(tc).Apply().Wait();
            TransactionContext.Trace.InlineTraces.Add(tc.Trace);
            CommitChangesAsync(TransactionContext.Trace).Wait();
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

            TransactionContext = PrepareTransactionContext(tx);
            ExecutiveForConsensus.SetTransactionContext(TransactionContext).Apply().Wait();
            var tc = PrepareTransactionContext(TransactionContext.Trace.InlineTransactions[0]);
            ExecutiveForToken.SetTransactionContext(tc).Apply().Wait();
            TransactionContext.Trace.InlineTraces.Add(tc.Trace);
            CommitChangesAsync(TransactionContext.Trace).Wait();
        }

        public void Vote(ECKeyPair voterKeyPair, ECKeyPair candidateKeyPair, ulong amount, int lockDays)
        {
            var tx = new Transaction
            {
                From = GetAddress(voterKeyPair),
                To = ConsensusContractAddress,
                IncrementId = MockSetup.NewIncrementId,
                MethodName = "Vote",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(candidateKeyPair.PublicKey.ToHex(), amount, lockDays))
            };
            var signer = new ECSigner();
            var signature = signer.Sign(voterKeyPair, tx.GetHash().DumpByteArray());
            tx.Sigs.Add(ByteString.CopyFrom(signature.SigBytes));

            TransactionContext = PrepareTransactionContext(tx);
            ExecutiveForConsensus.SetTransactionContext(TransactionContext).Apply().Wait();
            foreach (var inlineTx in TransactionContext.Trace.InlineTransactions.Where(t => t.To == TokenContractAddress))
            {
                var tc = PrepareTransactionContext(inlineTx);
                ExecutiveForToken.SetTransactionContext(tc).Apply().Wait();
                TransactionContext.Trace.InlineTraces.Add(tc.Trace);
            }
            foreach (var inlineTx in TransactionContext.Trace.InlineTransactions.Where(t => t.To == DividendsContractAddress))
            {
                var tc = PrepareTransactionContext(inlineTx);
                ExecutiveForDividends.SetTransactionContext(tc).Apply().Wait();
                TransactionContext.Trace.InlineTraces.Add(tc.Trace);
            }
            CommitChangesAsync(TransactionContext.Trace).Wait();
        }

        public void GetAllDividends(ECKeyPair ownerKeyPair)
        {
            var tx = new Transaction
            {
                From = GetAddress(ownerKeyPair),
                To = ConsensusContractAddress,
                IncrementId = MockSetup.NewIncrementId,
                MethodName = "GetAllDividends",
                Params = ByteString.CopyFrom(ParamsPacker.Pack())
            };
            var signer = new ECSigner();
            var signature = signer.Sign(ownerKeyPair, tx.GetHash().DumpByteArray());
            tx.Sigs.Add(ByteString.CopyFrom(signature.SigBytes));
            
            TransactionContext = PrepareTransactionContext(tx);
            ExecutiveForConsensus.SetTransactionContext(TransactionContext).Apply().Wait();

            foreach (var transaction in TransactionContext.Trace.InlineTransactions.Where(t => t.To == TokenContractAddress))
            {
                var tc = PrepareTransactionContext(transaction);
                ExecutiveForDividends.SetTransactionContext(tc).Apply().Wait();
                TransactionContext.Trace.InlineTraces.Add(tc.Trace);
            }
            foreach (var transaction in TransactionContext.Trace.InlineTransactions.Where(t => t.To == DividendsContractAddress))
            {
                var tc = PrepareTransactionContext(transaction);
                ExecutiveForDividends.SetTransactionContext(tc).Apply().Wait();
                TransactionContext.Trace.InlineTraces.Add(tc.Trace);

                foreach (var tx1 in tc.Trace.InlineTransactions.Where(t => t.To == TokenContractAddress))
                {
                    var tc1 = PrepareTransactionContext(tx1);
                    ExecutiveForToken.SetTransactionContext(tc1).Apply().Wait();
                    TransactionContext.Trace.InlineTraces.Add(tc1.Trace);
                }
            }
            
            CommitChangesAsync(TransactionContext.Trace).Wait();
        }

        #endregion Actions

        #endregion Election
        
        #region ABI (Public) Methods

        #region View Only Methods

        public string Symbol()
        {
            var tx = new Transaction
            {
                From = Sender,
                To = TokenContractAddress,
                IncrementId = MockSetup.NewIncrementId,
                MethodName = "Symbol",
                Params = ByteString.CopyFrom(ParamsPacker.Pack())
            };

            TransactionContext = new TransactionContext
            {
                Transaction = tx
            };
            ExecutiveForToken.SetTransactionContext(TransactionContext).Apply().Wait();
            TransactionContext.Trace.CommitChangesAsync(_mock.StateStore).Wait();
            return TransactionContext.Trace.RetVal?.Data.DeserializeToString();
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

            TransactionContext = new TransactionContext
            {
                Transaction = tx
            };
            ExecutiveForToken.SetTransactionContext(TransactionContext).Apply().Wait();
            TransactionContext.Trace.CommitChangesAsync(_mock.StateStore).Wait();
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

            TransactionContext = new TransactionContext
            {
                Transaction = tx
            };
            ExecutiveForToken.SetTransactionContext(TransactionContext).Apply().Wait();
            TransactionContext.Trace.CommitChangesAsync(_mock.StateStore).Wait();
            return TransactionContext.Trace.RetVal?.Data.DeserializeToUInt64()??0;
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

            TransactionContext = new TransactionContext
            {
                Transaction = tx
            };
            ExecutiveForToken.SetTransactionContext(TransactionContext).Apply().Wait();
            TransactionContext.Trace.CommitChangesAsync(_mock.StateStore).Wait();
            return TransactionContext.Trace.RetVal?.Data.DeserializeToUInt32()??0;
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

            TransactionContext = new TransactionContext()
            {
                Transaction = tx
            };
            ExecutiveForToken.SetTransactionContext(TransactionContext).Apply().Wait();
            TransactionContext.Trace.CommitChangesAsync(_mock.StateStore).Wait();
            return TransactionContext.Trace.RetVal?.Data.DeserializeToUInt64()??0;
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

            TransactionContext = new TransactionContext
            {
                Transaction = tx
            };
            ExecutiveForToken.SetTransactionContext(TransactionContext).Apply().Wait();
            TransactionContext.Trace.CommitChangesAsync(_mock.StateStore).Wait();
            return TransactionContext.Trace.RetVal?.Data.DeserializeToUInt64()??0;
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

            TransactionContext = PrepareTransactionContext(tx);
            ExecutiveForToken.SetTransactionContext(TransactionContext).Apply().Wait();
            CommitChangesAsync(TransactionContext.Trace).Wait();
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

            TransactionContext = PrepareTransactionContext(tx);
            ExecutiveForToken.SetTransactionContext(TransactionContext).Apply().Wait();
            CommitChangesAsync(TransactionContext.Trace).Wait();
        }

        public void TransferFrom(Address from, Address to, ulong amount)
        {
            var tx = new Transaction
            {
                From = Sender,
                To = TokenContractAddress,
                IncrementId = MockSetup.NewIncrementId,
                MethodName = "TransferFrom",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(from, to, amount))
            };

            TransactionContext = PrepareTransactionContext(tx);
            ExecutiveForToken.SetTransactionContext(TransactionContext).Apply().Wait();
            CommitChangesAsync(TransactionContext.Trace).Wait();
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

            TransactionContext = PrepareTransactionContext(tx);
            ExecutiveForToken.SetTransactionContext(TransactionContext).Apply().Wait();
            CommitChangesAsync(TransactionContext.Trace).Wait();
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

            TransactionContext = PrepareTransactionContext(tx);
            ExecutiveForToken.SetTransactionContext(TransactionContext).Apply().Wait();
            CommitChangesAsync(TransactionContext.Trace).Wait();
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

            TransactionContext = PrepareTransactionContext(tx);
            executive.SetTransactionContext(TransactionContext).Apply().Wait();
            CommitChangesAsync(TransactionContext.Trace).Wait();
            return TransactionContext.Trace.RetVal?.Data.DeserializeToPbMessage<Address>();
        }
        
        #endregion Actions

        #endregion ABI (Public) Methods

        private Address GetAddress(ECKeyPair keyPair)
        {
            return Address.FromPublicKey(_mock.ChainId.DumpByteArray(), keyPair.PublicKey);
        }
    }
}