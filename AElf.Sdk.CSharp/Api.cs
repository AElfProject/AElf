using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Kernel.Types.Transaction;
using AElf.SmartContract;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using AElf.Types.CSharp;
using AElf.Sdk.CSharp.ReadOnly;

namespace AElf.Sdk.CSharp
{
    /// <summary>
    /// Singleton that holds the smart contract API for interacting with the chain via the injected context.
    /// </summary>
    public class Api
    {
        private static Dictionary<string, IDataProvider> _dataProviders;
        private static ISmartContractContext _smartContractContext;
        private static ITransactionContext _transactionContext;
        private static ITransactionContext _lastCallContext;
        private static IBlockChain _blockChain;

        public static ProtobufSerializer Serializer { get; } = new ProtobufSerializer();

        #region Setters used by runner and executor

        public static void SetSmartContractContext(ISmartContractContext contractContext)
        {
            _smartContractContext = contractContext;
            _blockChain = contractContext.ChainService.GetBlockChain(contractContext.ChainId);
            _dataProviders = new Dictionary<string, IDataProvider> {{"", _smartContractContext.DataProvider}};
        }

        public static void SetTransactionContext(ITransactionContext transactionContext)
        {
            _transactionContext = transactionContext;
        }

        #endregion Setters used by runner and executor

        #region Getters used by contract

        #region Privileged API

        public static async Task DeployContractAsync(Address address, SmartContractRegistration registration)
        {
            Assert(_smartContractContext.ContractAddress.Equals(ContractZeroAddress));
            await _smartContractContext.SmartContractService.DeployContractAsync(ChainId, address, registration,
                false);
        }

        public static async Task UpdateContractAsync(Address address, SmartContractRegistration registration)
        {
            Assert(_smartContractContext.ContractAddress.Equals(ContractZeroAddress));
            await _smartContractContext.SmartContractService.UpdateContractAsync(ChainId, address, registration,
                false);
        }

        #endregion Privileged API

        public static Hash ChainId => _smartContractContext.ChainId.ToReadOnly();

        public static Address ContractZeroAddress => ContractHelpers.GetGenesisBasicContractAddress(ChainId);

        public static Address CrossChainContractAddress => ContractHelpers.GetCrossChainContractAddress(ChainId);

        public static Address AuthorizationContractAddress => ContractHelpers.GetAuthorizationContractAddress(ChainId);

        public static Address ResourceContractAddress => ContractHelpers.GetResourceContractAddress(ChainId);

        public static Address TokenContractAddress => ContractHelpers.GetTokenContractAddress(ChainId);

        public static Address ConsensusContractAddress => ContractHelpers.GetConsensusContractAddress(ChainId);

        public static Address DividendsContractAddress => ContractHelpers.GetDividendsContractAddress(ChainId);

        public static DateTime CurrentBlockTime => _transactionContext.CurrentBlockTime;

        public static Address Genesis => Address.Genesis;

        public static Block GetBlockByHeight(ulong height)
        {
            return (Block) _blockChain.GetBlockByHeightAsync(height, true).Result;
        }

        public static Hash GetPreviousBlockHash()
        {
            // TODO: Maybe use _blockChain to get
            return _transactionContext.PreviousBlockHash.ToReadOnly();
        }

        public static ulong GetCurrentHeight()
        {
            // TODO: Maybe use _blockChain to get
            return _transactionContext.BlockHeight;
        }

        public static Address GetContractAddress()
        {
            return _smartContractContext.ContractAddress.ToReadOnly();
        }

        public static byte[] RecoverPublicKey(byte[] signature, byte[] hash)
        {
            var cabBeRecovered = CryptoHelpers.RecoverPublicKey(signature, hash, out var publicKey);
            return !cabBeRecovered ? null : publicKey;
        }

        /// <summary>
        /// Recover the first public key signing this transaction.
        /// </summary>
        /// <returns></returns>
        public static byte[] RecoverPublicKey()
        {
            return RecoverPublicKey(GetTransaction().Sigs.First().ToByteArray(), GetTxnHash().DumpByteArray());
        }

        public static Miners GetMiners()
        {
            if (Call(ConsensusContractAddress, "GetRoundInfo"))
            {
                var round = GetCallResult().DeserializeToPbMessage<Round>();
                var miners = round.RealTimeMinersInfo.Keys.ToMiners();
                miners.TermNumber = round.MinersTermNumber;

                return miners;
            }
    
            throw new InternalError("Failed to current miners.\n" + _lastCallContext.Trace.StdErr);
        }
        
        public static ulong GetCurrentRoundNumber()
        {
            if (Call(ConsensusContractAddress, "GetCurrentRoundNumber"))
            {
                return GetCallResult().DeserializeToPbMessage<UInt64Value>().Value;
            }
            
            throw new InternalError("Failed to get current round number.\n" + _lastCallContext.Trace.StdErr);
        }

        public static ulong GetCurrentTermNumber()
        {
            if (Call(ConsensusContractAddress, "GetCurrentTermNumber"))
            {
                return GetCallResult().DeserializeToPbMessage<UInt64Value>().Value;
            }

            throw new InternalError("Failed to get current term number.\n" + _lastCallContext.Trace.StdErr);
        }

        public static TermSnapshot GetTermSnapshot(ulong termNumber)
        {
            if (Call(ConsensusContractAddress, "GetTermSnapshot", termNumber))
            {
                return GetCallResult().DeserializeToPbMessage<TermSnapshot>();
            }

            throw new InternalError($"Failed to get term snapshot of term {termNumber}.\n" +
                                    _lastCallContext.Trace.StdErr);
        }

        public static Address GetContractOwner()
        {
            if (Call(ContractZeroAddress, "GetContractOwner", _smartContractContext.ContractAddress))
            {
                return GetCallResult().DeserializeToPbMessage<Address>();
            }

            throw new InternalError("Failed to get owner of contract.\n" + _lastCallContext.Trace.StdErr);
        }

        public static IDataProvider GetDataProvider(string name)
        {
            if (_dataProviders.TryGetValue(name, out var dp))
                return dp;
            dp = _smartContractContext.DataProvider.GetDataProvider(name);
            _dataProviders.Add(name, dp);

            return dp;
        }

        private static Transaction GetTransaction()
        {
            return _transactionContext.Transaction.ToReadOnly();
        }

        public static Hash GetTxnHash()
        {
            return GetTransaction().GetHash();
        }

        public static Address GetFromAddress()
        {
            return GetTransaction().From.Clone();
        }

        public static Address GetToAddress()
        {
            return GetTransaction().To.Clone();
        }

        /// <summary>
        /// Return resource balance of from account.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="resourceType"></param>
        /// <returns></returns>
        public static ulong GetResourceBalance(Address address, ResourceType resourceType)
        {
            Assert(GetFromAddress().Equals(address), "Not authorized to check resource");
            Call(ResourceContractAddress, "GetResourceBalance", address, resourceType.ToString());
            return GetCallResult().DeserializeToPbMessage<UInt64Value>().Value;
        }

        /// <summary>
        /// Return token balance of from account.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static ulong GetTokenBalance(Address address)
        {
            Assert(GetFromAddress().Equals(address), "Not authorized to check resource");
            Call(TokenContractAddress, "BalanceOf", address);
            return GetCallResult().DeserializeToPbMessage<UInt64Value>().Value;
        }

        public static ulong GetBalanceOfDividendsContract()
        {
            Call(TokenContractAddress, "BalanceOf", DividendsContractAddress);
            return GetCallResult().DeserializeToPbMessage<UInt64Value>().Value;
        }

        #endregion Getters used by contract

        #region Transaction API

        public static void SendInline(Address contractAddress, string methodName, params object[] args)
        {
            _transactionContext.Trace.InlineTransactions.Add(new Transaction()
            {
                From = _transactionContext.Transaction.From,
                To = contractAddress,
                MethodName = methodName,
                Params = ByteString.CopyFrom(ParamsPacker.Pack(args))
            });
        }

        public static void SendDividends(params object[] args)
        {
            if (GetBalanceOfDividendsContract() > 0)
            {
                _transactionContext.Trace.InlineTransactions.Add(new Transaction()
                {
                    From = DividendsContractAddress,
                    To = TokenContractAddress,
                    MethodName = "Transfer",
                    Params = ByteString.CopyFrom(ParamsPacker.Pack(args))
                });
            }
        }

        /// <summary>
        /// Send transaction from current contract address
        /// </summary>
        /// <param name="contractAddress"></param>
        /// <param name="methodName"></param>
        /// <param name="args"></param>
        public static void SendInlineByContract(Address contractAddress, string methodName, params object[] args)
        {
            _transactionContext.Trace.InlineTransactions.Add(new Transaction()
            {
                From = GetContractAddress(),
                To = contractAddress,
                MethodName = methodName,
                Params = ByteString.CopyFrom(ParamsPacker.Pack(args))
            });
        }

        public static bool Call(Address contractAddress, string methodName, params object[] args)
        {
            _lastCallContext = new TransactionContext()
            {
                Transaction = new Transaction()
                {
                    From = _smartContractContext.ContractAddress,
                    To = contractAddress,
                    MethodName = methodName,
                    Params = ByteString.CopyFrom(ParamsPacker.Pack(args))
                },
                CurrentMiners = GetMiners()
            };

            var svc = _smartContractContext.SmartContractService;
            var ctxt = _lastCallContext;
            var chainId = ChainId;
            Task.Factory.StartNew(async () =>
            {
                var executive = await svc.GetExecutiveAsync(contractAddress, chainId);
                executive.SetDataCache(_dataProviders[""].StateCache);
                try
                {
                    // view only, write actions need to be sent via SendInline
                    await executive.SetTransactionContext(ctxt).Apply();
                }
                finally
                {
                    await svc.PutExecutiveAsync(chainId, contractAddress, executive);
                }
            }).Unwrap().Wait();

            // TODO: Maybe put readonly call trace into inlinetraces to record data access

            return _lastCallContext.Trace.IsSuccessful();
        }

        public static byte[] GetCallResult()
        {
            if (_lastCallContext != null)
            {
                return _lastCallContext.Trace.RetVal.Data.ToByteArray();
            }

            return new byte[] { };
        }

        public static bool VerifySignature(Transaction proposedTxn)
        {
            return new TxSignatureVerifier().Verify(proposedTxn);
        }

        public static bool VerifyTransaction(Hash txId, MerklePath merklePath, ulong parentChainHeight)
        {
            var scAddress = CrossChainContractAddress;

            if (scAddress == null)
            {
                throw new InternalError("No side chain contract was found.\n" + _lastCallContext.Trace.StdErr);
            }

            if (Call(scAddress, "VerifyTransaction", txId, merklePath, parentChainHeight))
            {
                return GetCallResult().DeserializeToPbMessage<BoolValue>().Value;
            }

            return false;
        }

        public static void LockToken(ulong amount)
        {
            SendInline(TokenContractAddress, "Transfer", GetContractAddress(), amount);
        }

        public static void UnlockToken(Address address, ulong amount)
        {
            SendInlineByContract(TokenContractAddress, "Transfer", address, amount);
        }

        public static void LockResource(ulong amount, ResourceType resourceType)
        {
            SendInline(ResourceContractAddress, "LockResource", GetContractAddress(), amount, resourceType);
        }

        public static void UnlockResource(ulong amount, ResourceType resourceType)
        {
            SendInlineByContract(ResourceContractAddress, "UnlockResource", GetFromAddress(), amount, resourceType);
        }

        #endregion Transaction API

        #region Utility API

        public static void Assert(bool asserted, string message = "Assertion failed!")
        {
            if (!asserted)
            {
                throw new AssertionError(message);
            }
        }

        public static void Equal<T>(T expected, T actual, string message = "Assertion failed!")
        {
            Assert(expected.Equals(actual), message);
        }

        internal static void FireEvent(LogEvent logEvent)
        {
            _transactionContext.Trace.Logs.Add(logEvent);
        }

        #endregion Utility API

        #region Diagonstics API

        public static void Sleep(int milliSeconds)
        {
            Thread.Sleep(milliSeconds);
        }

        #endregion Diagonstics API

        /// <summary>
        /// Generate txn not executed before next block. 
        /// </summary>
        /// <param name="deferredTxn"></param>
        public static void SendDeferredTransaction(Transaction deferredTxn)
        {
            _transactionContext.Trace.DeferredTransaction = deferredTxn.ToByteString();
        }

        /// <summary>
        /// Check authority of this transaction especially for multi signature ones.
        /// </summary>
        /// <param name="fromAddress">Valid transaction From address.</param>
        public static void CheckAuthority(Address fromAddress = null)
        {
            Assert(fromAddress == null || fromAddress.Equals(GetFromAddress()), "Not authorized transaction.");
            if (_transactionContext.Transaction.Sigs.Count == 1)
                // No need to verify signature again if it is not multi sig account.
                return;
            Call(AuthorizationContractAddress, "GetAuth", _transactionContext.Transaction.From);
            var auth = GetCallResult().DeserializeToPbMessage<Authorization>();

            // Get tx hash
            var hash = _transactionContext.Transaction.GetHash().DumpByteArray();

            // Get pub keys
            var publicKeys = new List<byte[]>();
            foreach (var sig in _transactionContext.Transaction.Sigs)
            {
                var publicKey = RecoverPublicKey(sig.ToByteArray(), hash);
                Assert (publicKey != null, "Invalid signature."); // this should never happen.
                publicKeys.Add(publicKey);
            }
            
            //todo review correctness
            uint provided = publicKeys
                .Select(pubKey => auth.Reviewers.FirstOrDefault(r => r.PubKey.ToByteArray().SequenceEqual(pubKey)))
                .Where(r => !(r is default(Reviewer))).Aggregate<Reviewer, uint>(0, (current, r) => current + r.Weight);
            Assert(provided >= auth.ExecutionThreshold, "Authorization failed without enough approval.");
        }

        public static void IsMiner(string err)
        {
            Assert(GetMiners().PublicKeys.Any(p => ByteArrayHelpers.FromHexString(p).BytesEqual(RecoverPublicKey())), err);
        }
        
        /// <summary>
        /// Create and propose a proposal. Proposer is current transaction from account.
        /// </summary>
        /// <param name="proposalName">Proposal name.</param>
        /// <param name="targetAddress">To address of packed transaction.</param>
        /// <param name="invokingMethod">The method to be invoked in packed transaction.</param>
        /// <param name="waitingPeriod">Expired time in second for proposal.</param>
        /// <param name="args">The arguments for packed transaction.</param>
        public static Hash Propose(string proposalName, double waitingPeriod, Address fromAddress,
            Address targetAddress, string invokingMethod, params object[] args)
        {
            // packed txn
            byte[] txnData = new Transaction
            {
                From = fromAddress,
                To = targetAddress,
                MethodName = invokingMethod,
                Params = ByteString.CopyFrom(ParamsPacker.Pack(args)),
                Type = TransactionType.MsigTransaction
            }.ToByteArray();
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan diff = CurrentBlockTime.AddSeconds(waitingPeriod).ToUniversalTime() - origin;

            Proposal proposal = new Proposal
            {
                MultiSigAccount = fromAddress,
                Name = proposalName,
                TxnData = ByteString.CopyFrom(txnData),
                ExpiredTime = diff.TotalSeconds,
                Status = ProposalStatus.ToBeDecided,
                Proposer = GetFromAddress()
            };
            SendInline(AuthorizationContractAddress, "Propose", proposal);
            return proposal.GetHash();
        }

        public static bool IsMultiSigAccount(Address address)
        {
            Call(AuthorizationContractAddress, "IsMultiSigAccount", address);
            return GetCallResult().DeserializeToPbMessage<BoolValue>().Value;
        }
    }
}