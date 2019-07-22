using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.Types;
using Anemonis.AspNetCore.JsonRpc;
using Anemonis.JsonRpc;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Type = System.Type;

namespace AElf.OS.Rpc.ChainController
{
    //TODO: remove this extensions
    internal static class ServiceExtensions
    {
        public static async Task<string[]> PublishTransactionsAsync(this ChainControllerRpcService s,
            string[] rawTransactions)
        {
            var txIds = new string[rawTransactions.Length];
            var transactions = new List<Transaction>();
            for (int i = 0; i < rawTransactions.Length; i++)
            {
                Transaction transaction;
                try
                {
                    var hexString = ByteArrayHelper.HexStringToByteArray(rawTransactions[i]);
                    transaction = Transaction.Parser.ParseFrom(hexString);
                }
                catch
                {
                    throw new JsonRpcServiceException(Error.InvalidTransaction,
                        Error.Message[Error.InvalidTransaction]);
                }
                
                if (!transaction.VerifySignature())
                {
                    throw new JsonRpcServiceException(Error.InvalidTransaction,
                        Error.Message[Error.InvalidTransaction]);
                }

                transactions.Add(transaction);
                txIds[i] = transaction.GetHash().ToHex();
            }

            await s.LocalEventBus.PublishAsync(new TransactionsReceivedEvent()
            {
                Transactions = transactions
            });
            return txIds;
        }

        internal static IDictionary<string, (JsonRpcRequestContract, MethodInfo, ParameterInfo[], string[])>
            GetRpcMethodContracts(this ChainControllerRpcService s)
        {
            var methods = s.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public);
            IDictionary<string, (JsonRpcRequestContract, MethodInfo, ParameterInfo[], string[])> contracts =
                new ConcurrentDictionary<string, (JsonRpcRequestContract, MethodInfo, ParameterInfo[], string[])>();

            foreach (var method in methods)
            {
                var attribute = method.GetCustomAttribute<JsonRpcMethodAttribute>();
                if (attribute == null)
                {
                    continue;
                }

                if (!(method.ReturnType == typeof(Task)) &&
                    !(method.ReturnType.IsGenericType &&
                      (method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))))
                {
                    continue;
                }

                var contract = default(JsonRpcRequestContract);
                var parameters = method.GetParameters();
                var parametersBindings = default(string[]);

                JsonRpcParametersType ParametersType() =>
                    // ReSharper disable once PossibleNullReferenceException
                    (JsonRpcParametersType) typeof(JsonRpcMethodAttribute).GetProperty("ParametersType",
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        ?.GetValue(attribute, null);

                int[] ParameterPositions() =>
                    (int[]) typeof(JsonRpcMethodAttribute).GetProperty("ParameterPositions",
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        ?.GetValue(attribute, null);

                string[] ParameterNames() =>
                    (string[]) typeof(JsonRpcMethodAttribute).GetProperty("ParameterNames",
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        ?.GetValue(attribute, null);

                string MethodName() =>
                    (string) typeof(JsonRpcMethodAttribute).GetProperty("MethodName",
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        ?.GetValue(attribute, null);

                switch (ParametersType())
                {
                    case JsonRpcParametersType.ByPosition:
                    {
                        var parameterPositions = ParameterPositions();

                        if (parameterPositions.Length != parameters.Length)
                        {
                            continue;
                        }

                        if (!Enumerable.Range(0, parameterPositions.Length).All(i => parameterPositions.Contains(i)))
                        {
                            continue;
                        }

                        var parametersContract = new Type[parameters.Length];
                        for (var i = 0; i < parameters.Length; i++)
                        {
                            parametersContract[i] = parameters[i].ParameterType;
                        }

                        contract = new JsonRpcRequestContract(parametersContract);
                    }
                        break;
                    case JsonRpcParametersType.ByName:
                    {
                        var parameterNames = ParameterNames();

                        if (parameterNames.Length != parameters.Length)
                        {
                            continue;
                        }

                        if (parameterNames.Length != parameterNames.Distinct(StringComparer.Ordinal).Count())
                        {
                            continue;
                        }

                        var parametersContract =
                            new Dictionary<string, Type>(parameters.Length, StringComparer.Ordinal);

                        parametersBindings = new string[parameters.Length];

                        for (var i = 0; i < parameters.Length; i++)
                        {
                            parametersContract[parameterNames[i]] = parameters[i].ParameterType;
                            parametersBindings[i] = parameterNames[i];
                        }

                        contract = new JsonRpcRequestContract(parametersContract);
                    }
                        break;
                    default:
                    {
                        if (parameters.Length != 0)
                        {
                            continue;
                        }

                        contract = new JsonRpcRequestContract();
                    }
                        break;
                }

                contracts[MethodName()] = (contract, method, parameters, parametersBindings);
            }

            return contracts;
        }

        internal static async Task<TransactionResult> GetTransactionResult(this ChainControllerRpcService s,
            Hash transactionId)
        {
            // in storage
            var res = await s.TransactionResultQueryService.GetTransactionResultAsync(transactionId);
            if (res != null)
            {
                return res;
            }

            // in tx pool
            var receipt = await s.TxHub.GetTransactionReceiptAsync(transactionId);
            if (receipt != null)
            {
                return new TransactionResult
                {
                    TransactionId = receipt.TransactionId,
                    Status = TransactionResultStatus.Pending
                };
            }

            // not existed
            return new TransactionResult
            {
                TransactionId = transactionId,
                Status = TransactionResultStatus.NotExisted
            };
        }

// No call reference 
//        internal static async Task<string> GetTransactionParameters(this ChainControllerRpcService s, Transaction tx)
//        {
//            string output = null;
//            try
//            {
//                var chainContext = await s.GetChainContextAsync();
//
//                output = await s.TransactionReadOnlyExecutionService.GetTransactionParametersAsync(
//                    chainContext, tx);
//            }
//            catch (InvalidCastException ex)
//            {
//                s.Logger.LogWarning($"Unsupported type conversion error： {ex}");
//            }
//
//            return output;
//        }

        internal static async Task<long> GetCurrentChainHeight(this ChainControllerRpcService s)
        {
            var chainContext = await s.BlockchainService.GetChainAsync();
            return chainContext.BestChainHeight;
        }

        internal static async Task<Block> GetBlockAtHeight(this ChainControllerRpcService s, long height)
        {
            return await s.BlockchainService.GetBlockByHeightInBestChainBranchAsync(height);
        }

        internal static async Task<JObject> GetTransactionPoolStatusAsync(this ChainControllerRpcService s)
        {
            return new JObject
            {
                ["Queued"] = await s.TxHub.GetTransactionPoolSizeAsync()
            };
        }

        internal static async Task<byte[]> CallReadOnly(this ChainControllerRpcService s, Transaction tx)
        {
            var chainContext = await s.GetChainContextAsync();

            var trace = await s.TransactionReadOnlyExecutionService.ExecuteAsync(chainContext, tx, TimestampHelper.GetUtcNow());

            if (!string.IsNullOrEmpty(trace.Error))
                throw new Exception(trace.Error);

            return trace.ReturnValue.ToByteArray();
        }

        private static async Task<ChainContext> GetChainContextAsync(this ChainControllerRpcService s)
        {
            var chain = await s.BlockchainService.GetChainAsync();
            var chainContext = new ChainContext()
            {
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight
            };
            return chainContext;
        }

        internal static async Task<byte[]> GetFileDescriptorSetAsync(this ChainControllerRpcService s, Address address)
        {
            var chain = await s.BlockchainService.GetChainAsync();
            var chainContext = new ChainContext()
            {
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight
            };

            return await s.TransactionReadOnlyExecutionService.GetFileDescriptorSetAsync(chainContext, address);
        }

        internal static async Task<Block> GetBlock(this ChainControllerRpcService s, Hash blockHash)
        {
            return (Block) await s.BlockchainService.GetBlockByHashAsync(blockHash);
        }

        #region Cross chain

        /*
        internal static async Task<MerklePath> GetTxRootMerklePathInParentChain(this Svc s, ulong height)
        {
            var merklePath = await s.CrossChainInfoReader.GetTxRootMerklePathInParentChainAsync(height);
            if (merklePath != null)
                return merklePath;
            throw new Exception();
        }

        internal static async Task<JObject> GetIndexedSideChainBlockInfo(this Svc s, ulong height)
        {
            var res = new JObject();
            var indexedSideChainBlockInfoResult = await s.CrossChainInfoReader.GetIndexedSideChainBlockInfoResult(height);
            if (indexedSideChainBlockInfoResult == null)
                return res;
            foreach (var sideChainIndexedInfo in indexedSideChainBlockInfoResult.SideChainBlockData)
            {
                res.Add(sideChainIndexedInfo..DumpBase58(), new JObject
                {
                    {"Height", sideChainIndexedInfo.Height},
                    {"BlockHash", sideChainIndexedInfo.BlockHeaderHash.ToHex()},
                    {"TransactionMerkleTreeRoot", sideChainIndexedInfo.TransactionMKRoot.ToHex()}
                });
            }

            return res;
        }

        internal static async Task<ParentChainBlockData> GetParentChainBlockInfo(this Svc s, ulong height)
        {
            var parentChainBlockInfo = await s.CrossChainInfoReader.GetBoundParentChainBlockInfoAsync(height);
            if (parentChainBlockInfo != null)
                return parentChainBlockInfo;
            throw new Exception();
        }

        internal static async Task<ulong> GetBoundParentChainHeight(this Svc s, ulong height)
        {
            var parentHeight = await s.CrossChainInfoReader.GetBoundParentChainHeightAsync(height);
            if (parentHeight != 0)
                return parentHeight;
            throw new Exception();
        }
        */

        #endregion

        #region Proposal

        /*
        internal static async Task<Proposal> GetProposal(this Svc s, Hash proposalHash)
        {
            return await s.AuthorizationInfoReader.GetProposal(proposalHash);
        }

        internal static async Task<Authorization> GetAuthorization(this Svc s, Address msig)
        {
            return await s.AuthorizationInfoReader.GetAuthorization(msig);
        }
        */

        #endregion

        /*
        internal static async Task<int> GetRollBackTimesAsync(this Svc s)
        {
            return await Task.FromResult(s.BlockSynchronizer.RollBackTimes);
        }
        */
    }
}