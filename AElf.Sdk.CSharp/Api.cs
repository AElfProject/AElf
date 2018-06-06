using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

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
        private static ITransactionContext _lastInlineCallContext;

        #region Setters used by runner and executor

        public static void SetSmartContractContext(ISmartContractContext contractContext)
        {
            _smartContractContext = contractContext;
            _dataProviders = new Dictionary<string, IDataProvider>()
            {
                {"", _smartContractContext.DataProvider}
            };
        }

        public static void SetTransactionContext(ITransactionContext transactionContext)
        {
            _transactionContext = transactionContext;
        }

        #endregion Setters used by runner and executor

        #region Getters used by contract

        #region Privileged API
        public static async Task DeployContractAsync(Hash address, SmartContractRegistration registration)
        {
            await _smartContractContext.SmartContractService.DeployContractAsync(address, registration);
        }

        #endregion Privileged API

        public static Hash GetChainId()
        {
            return _smartContractContext.ChainId;
        }

        public static Hash GetContractAddress()
        {
            return _smartContractContext.ContractAddress;
        }

        public static IDataProvider GetDataProvider(string name)
        {
            if (!_dataProviders.TryGetValue(name, out var dp))
            {
                dp = _smartContractContext.DataProvider.GetDataProvider(name);
                _dataProviders.Add(name, dp);
            }

            return dp;
        }

        public static ITransaction GetTransaction()
        {
            return _transactionContext.Transaction;
        }

        public static void RaiseEvent(LogEvent logEvent)
        {
            // TODO: Improve
            _transactionContext.Trace.Logs.Add(logEvent);
        }

        #endregion Getters used by contract

        #region Transaction API
        public static bool Call(Hash contractAddress, string methodName, byte[] args)
        {
            _lastInlineCallContext = new TransactionContext()
            {
                Transaction = new Transaction()
                {
                    From = _smartContractContext.ContractAddress,
                    To = contractAddress,
                    // TODO: Get increment id from AccountDataContext
                    IncrementId = ulong.MinValue,
                    MethodName = methodName,
                    Params = ByteString.CopyFrom(args)
                }
            };

            Task.Factory.StartNew(async () =>
            {
                var executive = await _smartContractContext.SmartContractService.GetExecutiveAsync(contractAddress, _smartContractContext.ChainId);
                await executive.SetTransactionContext(_lastInlineCallContext).Apply();
            }).Unwrap().Wait();

            _transactionContext.Trace.Logs.AddRange(_lastInlineCallContext.Trace.Logs);

            // TODO: Put inline transactions into Transaction Result of calling transaction

            // True: success
            // False: error
            return string.IsNullOrEmpty(_lastInlineCallContext.Trace.StdErr);
        }

        public static Any GetCallResult()
        {
            if (_lastInlineCallContext == null)
            {
                return _lastInlineCallContext.Trace.RetVal;
            }
            return new Any();
        }

        #endregion Transaction API

    }
}