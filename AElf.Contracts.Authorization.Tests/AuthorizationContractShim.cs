//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using AElf.Common;
//using AElf.Cryptography;
//using AElf.Cryptography.ECDSA;
//using AElf.SmartContract;
//using AElf.Kernel;
//using AElf.Types.CSharp;
//using Google.Protobuf;
//
//namespace AElf.Contracts.Authorization.Tests
//{
//    public class AuthorizationContractShim
//    {
//        private byte[] _chainId;
//        
//        private MockSetup _mock;
//        public IExecutive _executive;
//        public IExecutive Executive {
//            get
//            {
//                _executive?.SetDataCache(new Dictionary<StatePath, StateCache>());
//                return _executive;
//            }
//      
//            set => _executive = value;
//        }
//
//        public ITransactionContext TransactionContext { get; private set; }
//
//        public Address Sender { get; } = Address.FromString("sender");
//        public Address AuthorizationContractAddress { get; set; }
//
//        public AuthorizationContractShim(MockSetup mock, Address authorizationContractAddress, byte[] chainId)
//        {
//            _chainId = chainId;
//            _mock = mock;
//            AuthorizationContractAddress = authorizationContractAddress;
//            Init();
//        }
//
//        private void Init()
//        {
//            var task = _mock.GetExecutiveAsync(AuthorizationContractAddress);
//            task.Wait();
//            Executive = task.Result;
//        }
//        
//        private async Task CommitChangesAsync(TransactionTrace trace)
//        {
//            await trace.SmartCommitChangesAsync(_mock.StateProviderFactory.CreateStateManager());
//        }
//
//        public async Task<byte[]> CreateMSigAccount(Kernel.Authorization authorization)
//        {
//            var tx = new Transaction
//            {
//                From = Sender,
//                To = AuthorizationContractAddress,
//                MethodName = "CreateMultiSigAccount",
//                Params = ByteString.CopyFrom(ParamsPacker.Pack(authorization))
//            };
//            TransactionContext = new TransactionContext()
//            {
//                Transaction = tx
//            };
//            await Executive.SetTransactionContext(TransactionContext).Apply();
//            await CommitChangesAsync(TransactionContext.Trace);
//            return TransactionContext.Trace.RetVal?.Data.DeserializeToBytes();
//        }
//
//        public async Task<byte[]> Propose(Proposal proposal, ECKeyPair sender)
//        {
//            try
//            {
//                var tx = new Transaction
//                {
//                    From = Address.FromPublicKey(sender.PublicKey),
//                    To = AuthorizationContractAddress,
//                    MethodName = "Propose",
//                    Params = ByteString.CopyFrom(ParamsPacker.Pack(proposal))
//                };
//                
//                var signature = CryptoHelpers.SignWithPrivateKey(sender.PrivateKey, tx.GetHash().DumpByteArray());
//                
//                tx.Sigs.Add(ByteString.CopyFrom(signature));
//                
//                TransactionContext = new TransactionContext
//                {
//                    Transaction = tx
//                };
//                
//                await Executive.SetTransactionContext(TransactionContext).Apply();
//                await CommitChangesAsync(TransactionContext.Trace);
//                return TransactionContext.Trace.RetVal?.Data.DeserializeToBytes();
//            }
//            catch (Exception)
//            {
//                return null;
//            }
//        }
//
//        public async Task<bool> SayYes(Approval approval, ECKeyPair sender)
//        {
//            try
//            {
//                var tx = new Transaction
//                {
//                    From = Address.FromPublicKey(sender.PublicKey),
//                    To = AuthorizationContractAddress,
//                    MethodName = "SayYes",
//                    Params = ByteString.CopyFrom(ParamsPacker.Pack(approval))
//                };
//                var signature = CryptoHelpers.SignWithPrivateKey(sender.PrivateKey, tx.GetHash().DumpByteArray());
//                
//                tx.Sigs.Add(ByteString.CopyFrom(signature));
//                TransactionContext = new TransactionContext()
//                {
//                    Transaction = tx
//                };
//                await Executive.SetTransactionContext(TransactionContext).Apply();
//                await CommitChangesAsync(TransactionContext.Trace);
//                return TransactionContext.Trace.RetVal?.Data.DeserializeToBool() ?? false;
//            }
//            catch (Exception)
//            {
//                return false;
//            }
//        }
//
//        public async Task<Transaction> Release(Hash proposalHash, ECKeyPair sender)
//        {
//            try
//            {
//                var tx = new Transaction
//                {
//                    From = Address.FromPublicKey(sender.PublicKey),
//                    To = AuthorizationContractAddress,
//                    MethodName = "Release",
//                    Params = ByteString.CopyFrom(ParamsPacker.Pack(proposalHash))
//                };
//                TransactionContext = new TransactionContext()
//                {
//                    Transaction = tx
//                };
//                var signature = CryptoHelpers.SignWithPrivateKey(sender.PrivateKey, tx.GetHash().DumpByteArray());
//                
//                tx.Sigs.Add(ByteString.CopyFrom(signature));
//                TransactionContext = new TransactionContext()
//                {
//                    Transaction = tx
//                };
//                await Executive.SetTransactionContext(TransactionContext).Apply();
//
//                if (TransactionContext.Trace.DeferredTransaction == ByteString.Empty)
//                {
//                    TransactionContext.Trace.DeferredTransaction = null;
//                }
//                await CommitChangesAsync(TransactionContext.Trace);
//                return TransactionContext.Trace.DeferredTransaction != null
//                    ? Transaction.Parser.ParseFrom(TransactionContext.Trace.DeferredTransaction)
//                    : null;
//            }
//            catch (Exception)
//            {
//                return null;
//            }
//        }
//    }
//}