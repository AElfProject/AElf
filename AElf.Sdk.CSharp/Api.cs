﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using AElf.Types.CSharp;

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

        public static ProtobufSerializer Serializer { get; } = new ProtobufSerializer();

        #region Setters used by runner and executor

        public static void SetSmartContractContext(ISmartContractContext contractContext)
        {
            _smartContractContext = contractContext;
            _dataProviders = new Dictionary<string, IDataProvider>();
            _dataProviders.Add("", _smartContractContext.DataProvider);
        }

        public static void SetTransactionContext(ITransactionContext transactionContext)
        {
            _transactionContext = transactionContext;
        }

        #endregion Setters used by runner and executor

        #region Getters used by contract

        #region Privileged API

        public static void DeployContract(Hash address, SmartContractRegistration registration)
        {
            Assert(_smartContractContext.ContractAddress.Equals(GetContractZeroAddress()));
            var task = _smartContractContext.SmartContractService.DeployContractAsync(GetChainId(), address,
                registration, false);
            task.Wait();
        }

        public static async Task DeployContractAsync(Hash address, SmartContractRegistration registration)
        {
            Assert(_smartContractContext.ContractAddress.Equals(GetContractZeroAddress()));
            await _smartContractContext.SmartContractService.DeployContractAsync(GetChainId(), address, registration,
                false);
        }

        #endregion Privileged API

        public static Hash GetChainId()
        {
            return _smartContractContext.ChainId;
        }

        public static Hash GetContractZeroAddress()
        {
            return new Hash(_smartContractContext.ChainId.CalculateHashWith(Globals.SmartContractZeroIdString)).ToAccount();
        }

        public static Hash GetPreviousBlockHash()
        {
            return _transactionContext.PreviousBlockHash;
        }

        public static Hash GetContractAddress()
        {
            return _smartContractContext.ContractAddress;
        }

        public static Hash GetContractOwner()
        {
            if (Call(GetContractZeroAddress(), "GetContractOwner",
                ParamsPacker.Pack(_smartContractContext.ContractAddress)))
            {
                return GetCallResult().DeserializeToPbMessage<Hash>();
            }
            throw new InternalError("Failed to get owner of contract.");
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

        public static bool TryCall(Hash contractAddress, string methodName, byte[] args)
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
                var executive =
                    await _smartContractContext.SmartContractService.GetExecutiveAsync(contractAddress,
                        _smartContractContext.ChainId);
                // Inline calls are not auto-committed.
                await executive.SetTransactionContext(_lastInlineCallContext).Apply(false);
            }).Unwrap().Wait();

            if (_lastInlineCallContext.Trace.IsSuccessful())
            {
                _transactionContext.Trace.Logs.AddRange(_lastInlineCallContext.Trace.Logs);
            }

            // True: success
            // False: error
            return _lastInlineCallContext.Trace.IsSuccessful();
        }

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
                var executive =
                    await _smartContractContext.SmartContractService.GetExecutiveAsync(contractAddress,
                        _smartContractContext.ChainId);
                // Inline calls are not auto-committed.
                await executive.SetTransactionContext(_lastInlineCallContext).Apply(false);
            }).Unwrap().Wait();

            _transactionContext.Trace.Logs.AddRange(_lastInlineCallContext.Trace.Logs);

            // TODO: Put inline transactions into Transaction Result of calling transaction

            // True: success
            // False: error
            return _lastInlineCallContext.Trace.IsSuccessful();
        }

        public static byte[] GetCallResult()
        {
            if (_lastInlineCallContext != null)
            {
                return _lastInlineCallContext.Trace.RetVal.ToByteArray();
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