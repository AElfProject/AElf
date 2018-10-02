using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.SmartContract;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using AElf.Types.CSharp;
using AElf.Sdk.CSharp.ReadOnly;
using Globals = AElf.Kernel.Globals;

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

        public static ProtobufSerializer Serializer { get; } = new ProtobufSerializer();

        #region Setters used by runner and executor

        public static void SetSmartContractContext(ISmartContractContext contractContext)
        {
            _smartContractContext = contractContext;
            _dataProviders = new Dictionary<string, IDataProvider> {{"", _smartContractContext.DataProvider}};
        }

        public static void SetTransactionContext(ITransactionContext transactionContext)
        {
            _transactionContext = transactionContext;
        }

        #endregion Setters used by runner and executor

        #region Getters used by contract

        #region Privileged API

        public static void DeployContract(Address address, SmartContractRegistration registration)
        {
            Assert(_smartContractContext.ContractAddress.Equals(GetContractZeroAddress()));
            var task = _smartContractContext.SmartContractService.DeployContractAsync(GetChainId(), address,
                registration, false);
            task.Wait();
        }

        public static async Task DeployContractAsync(Address address, SmartContractRegistration registration)
        {
            Assert(_smartContractContext.ContractAddress.Equals(GetContractZeroAddress()));
            await _smartContractContext.SmartContractService.DeployContractAsync(GetChainId(), address, registration,
                false);
        }

        #endregion Privileged API

        public static Hash GetChainId()
        {
            return _smartContractContext.ChainId.ToReadOnly();
        }

        public static Address GetContractZeroAddress()
        {
            return Address.FromBytes(_smartContractContext.ChainId.CalculateHashWith(Globals.GenesisBasicContract));
        }

        public static Hash GetPreviousBlockHash()
        {
            return _transactionContext.PreviousBlockHash.ToReadOnly();
        }
        
        public static ulong GetCurerntHeight()
        {
            return _transactionContext.BlockHeight;
        }

        public static Address GetContractAddress()
        {
            return _smartContractContext.ContractAddress.ToReadOnly();
        }

        public static Address GetContractOwner()
        {
            if (Call(GetContractZeroAddress(), "GetContractOwner",
                ParamsPacker.Pack(_smartContractContext.ContractAddress)))
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

        public static Transaction GetTransaction()
        {
            return _transactionContext.Transaction.ToReadOnly();
        }

        #endregion Getters used by contract

        #region Transaction API

        public static void SendInline(Address contractAddress, string methodName, params object[] args)
        {
            _transactionContext.Trace.InlineTransactions.Add(new Transaction()
            {
                From = _transactionContext.Transaction.From,
                To=contractAddress,
                MethodName = methodName,
                Params = ByteString.CopyFrom(ParamsPacker.Pack(args))
            });
        }

        public static bool Call(Address contractAddress, string methodName, byte[] args)
        {
            _lastCallContext = new TransactionContext()
            {
                Transaction = new Transaction()
                {
                    From = _smartContractContext.ContractAddress,
                    To = contractAddress,
                    MethodName = methodName,
                    Params = ByteString.CopyFrom(args)
                }
            };

            var svc = _smartContractContext.SmartContractService;
            var ctxt = _lastCallContext;
            var chainId = _smartContractContext.ChainId;
            Task.Factory.StartNew(async () =>
            {
                var executive = await svc.GetExecutiveAsync(contractAddress, chainId);
                try
                {
                    // view only, write actions need to be sent via SendInline
                    await executive.SetTransactionContext(ctxt).Apply();
                }
                finally
                {
                    await svc.PutExecutiveAsync(contractAddress, executive);    
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

        #endregion Transaction API

        #region Utility API

        public static void Assert(bool asserted, string message = "Assertion failed!")
        {
            if (!asserted)
            {
                throw new AssertionError(message);
            }
        }

        internal static void FireEvent(LogEvent logEvent)
        {
            _transactionContext.Trace.Logs.Add(logEvent);
        }

        #endregion Utility API

        #region Diagonstics API

        public static void Sleep(int milliSedonds)
        {
            Thread.Sleep(milliSedonds);
        }

        #endregion Diagonstics API
    }
}