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
        public IExecutive Executive { get; set; }

        public TransactionContext TransactionContext { get; private set; }

        public Address Sender
        {
            get => Address.Zero;
        }
        
        public Address ConsensusContractAddress { get; set; }

        public ConsensusContractShim(MockSetup mock)
        {
            _mock = mock;
            Init();
        }

        private void Init()
        {
            DeployConsensusContractAsync().Wait();
            var task = _mock.GetExecutiveAsync(ConsensusContractAddress);
            task.Wait();
            Executive = task.Result;
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
            ConsensusContractAddress = Address.FromBytes(tc.Trace.RetVal.ToFriendlyBytes());
        }

        #region ABI (Public) Methods

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

            TransactionContext = new TransactionContext()
            {
                Transaction = tx
            };
            Executive.SetTransactionContext(TransactionContext).Apply().Wait();
            TransactionContext.Trace.CommitChangesAsync(_mock.StateStore).Wait();
            return TransactionContext.Trace.RetVal?.Data.DeserializeToBool();
        }

        public Tickets GetTicketsInfo(ECKeyPair keyPair)
        {
            var tx = new Transaction
            {
                From = GetAddress(keyPair),
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
            
            Executive.SetTransactionContext(TransactionContext).Apply().Wait();
            TransactionContext.Trace.CommitChangesAsync(_mock.StateStore).Wait();
            return TransactionContext.Trace.RetVal?.Data.DeserializeToPbMessage<Tickets>();
        }

        public ulong TotalSupply()
        {
            var tx = new Transaction
            {
                From = Sender,
                To = ConsensusContractAddress,
                IncrementId = MockSetup.NewIncrementId,
                MethodName = "TotalSupply",
                Params = ByteString.CopyFrom(ParamsPacker.Pack())
            };

            TransactionContext = new TransactionContext()
            {
                Transaction = tx
            };
            Executive.SetTransactionContext(TransactionContext).Apply().Wait();
            TransactionContext.Trace.CommitChangesAsync(_mock.StateStore).Wait();
            return TransactionContext.Trace.RetVal?.Data.DeserializeToUInt64()??0;
        }

        public uint Decimals()
        {
            var tx = new Transaction
            {
                From = Sender,
                To = ConsensusContractAddress,
                IncrementId = MockSetup.NewIncrementId,
                MethodName = "Decimals",
                Params = ByteString.CopyFrom(ParamsPacker.Pack())
            };

            TransactionContext = new TransactionContext()
            {
                Transaction = tx
            };
            Executive.SetTransactionContext(TransactionContext).Apply().Wait();
            TransactionContext.Trace.CommitChangesAsync(_mock.StateStore).Wait();
            return TransactionContext.Trace.RetVal?.Data.DeserializeToUInt32()??0;
        }

        public ulong BalanceOf(Hash owner)
        {
            var tx = new Transaction
            {
                From = Sender,
                To = ConsensusContractAddress,
                IncrementId = MockSetup.NewIncrementId,
                MethodName = "BalanceOf",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(owner))
            };

            TransactionContext = new TransactionContext()
            {
                Transaction = tx
            };
            Executive.SetTransactionContext(TransactionContext).Apply().Wait();
            TransactionContext.Trace.CommitChangesAsync(_mock.StateStore).Wait();
            return TransactionContext.Trace.RetVal?.Data.DeserializeToUInt64()??0;
        }

        public ulong Allowance(Address owner, Address spender)
        {
            var tx = new Transaction
            {
                From = Sender,
                To = ConsensusContractAddress,
                IncrementId = MockSetup.NewIncrementId,
                MethodName = "Allowance",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(owner, spender))
            };

            TransactionContext = new TransactionContext()
            {
                Transaction = tx
            };
            Executive.SetTransactionContext(TransactionContext).Apply().Wait();
            TransactionContext.Trace.CommitChangesAsync(_mock.StateStore).Wait();
            return TransactionContext.Trace.RetVal?.Data.DeserializeToUInt64()??0;
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
            Executive.SetTransactionContext(TransactionContext).Apply().Wait();
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
            Executive.SetTransactionContext(TransactionContext).Apply().Wait();
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
            Executive.SetTransactionContext(TransactionContext).Apply().Wait();
            CommitChangesAsync(TransactionContext.Trace).Wait();
        }
        
        public void TransferFrom(Address from, Address to, ulong amount)
        {
            var tx = new Transaction
            {
                From = Sender,
                To = ConsensusContractAddress,
                IncrementId = MockSetup.NewIncrementId,
                MethodName = "TransferFrom",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(from, to, amount))
            };

            TransactionContext = PrepareTransactionContext(tx);
            Executive.SetTransactionContext(TransactionContext).Apply().Wait();
            CommitChangesAsync(TransactionContext.Trace).Wait();
        }

        public void Approve(Address spender, ulong amount)
        {
            var tx = new Transaction
            {
                From = Sender,
                To = ConsensusContractAddress,
                IncrementId = MockSetup.NewIncrementId,
                MethodName = "Approve",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(spender, amount))
            };

            TransactionContext = PrepareTransactionContext(tx);
            Executive.SetTransactionContext(TransactionContext).Apply().Wait();
            CommitChangesAsync(TransactionContext.Trace).Wait();
        }

        public void UnApprove(Address spender, ulong amount)
        {
            var tx = new Transaction
            {
                From = Sender,
                To = ConsensusContractAddress,
                IncrementId = MockSetup.NewIncrementId,
                MethodName = "UnApprove",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(spender, amount))
            };

            TransactionContext = PrepareTransactionContext(tx);
            Executive.SetTransactionContext(TransactionContext).Apply().Wait();
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
                Params = ByteString.CopyFrom(ParamsPacker.Pack(ConsensusContractAddress))
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