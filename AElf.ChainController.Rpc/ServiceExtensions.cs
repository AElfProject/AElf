using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.JsonRpc;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AElf.Configuration;
using AElf.Kernel;
using AElf.Common;
using AElf.Configuration.Config.Chain;
using AElf.SmartContract;
using Community.AspNetCore.JsonRpc;
using Google.Protobuf;
using Svc = AElf.ChainController.Rpc.ChainControllerRpcService;

namespace AElf.ChainController.Rpc
{
    internal static class ServiceExtensions
    {
        internal static IDictionary<string, (JsonRpcRequestContract, MethodInfo, ParameterInfo[], string[])>
            GetRpcMethodContracts(this Svc s)
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

                Func<JsonRpcParametersType> ParametersType = () =>
                    (JsonRpcParametersType) typeof(JsonRpcMethodAttribute).GetProperty("ParametersType",
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        ?.GetValue(attribute, null);
                Func<int[]> ParameterPositions = () =>
                    (int[]) typeof(JsonRpcMethodAttribute).GetProperty("ParameterPositions",
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        ?.GetValue(attribute, null);
                Func<string[]> ParameterNames = () =>
                    (string[]) typeof(JsonRpcMethodAttribute).GetProperty("ParameterNames",
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        ?.GetValue(attribute, null);
                Func<string> MethodName = () =>
                    (string) typeof(JsonRpcMethodAttribute)
                        .GetProperty("MethodName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
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

                        if (!Enumerable.Range(0, parameterPositions.Length).All(i =>
                            parameterPositions.Contains(i)
                        ))
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

        internal static async Task<IMessage> GetContractAbi(this Svc s, Address address)
        {
            return await s.SmartContractService.GetAbiAsync(address);
        }

        internal static async Task<ulong> GetIncrementId(this Svc s, Address addr)
        {
            try
            {
                // ReSharper disable once InconsistentNaming
//                var idInDB = (await s.AccountContextService.GetAccountDataContext(addr, ByteArrayHelpers.FromHexString(NodeConfig.Instance.ChainId)))
//                    .IncrementId;
//                var idInPool = s.TxPool.GetIncrementId(addr);
//
//                return Math.Max(idInDB, idInPool);
                return ulong.MaxValue;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        internal static async Task<Transaction> GetTransaction(this Svc s, Hash txId)
        {
            var r = await s.TxHub.GetReceiptAsync(txId);
            return r?.Transaction;
        }

        internal static async Task<TransactionResult> GetTransactionResult(this Svc s, Hash txHash)
        {
            var res = await s.TransactionResultService.GetResultAsync(txHash);
            return res;
        }

        internal static async Task<TransactionTrace> GetTransactionTrace(this Svc s, Hash txHash, ulong height)
        {
            var b = await s.GetBlockAtHeight(height);
            if (b == null)
            {
                return null;
            }

            var prodAddr = Address.FromRawBytes(b.Header.P.ToByteArray());
            var res = await s.TransactionTraceManager.GetTransactionTraceAsync(txHash,
                HashHelpers.GetDisambiguationHash(height, prodAddr));
            return res;
        }

        internal static Address GetGenesisContractHash(this Svc s, SmartContractType contractType)
        {
            return s.ChainCreationService.GenesisContractHash(Hash.LoadHex(ChainConfig.Instance.ChainId), contractType);
        }

        internal static async Task<IEnumerable<string>> GetTransactionParameters(this Svc s, Transaction tx)
        {
            return await s.SmartContractService.GetInvokingParams(tx);
        }

        internal static async Task<ulong> GetCurrentChainHeight(this Svc s)
        {
            var chainContext = await s.ChainContextService.GetChainContextAsync(Hash.LoadHex(ChainConfig.Instance.ChainId));
            return chainContext.BlockHeight;
        }

        internal static async Task<Block> GetBlockAtHeight(this Svc s, ulong height)
        {
            var blockchain = s.ChainService.GetBlockChain(Hash.LoadHex(ChainConfig.Instance.ChainId));
            return (Block) await blockchain.GetBlockByHeightAsync(height);
        }

        internal static async Task<ulong> GetTransactionPoolSize(this Svc s)
        {
            return (ulong)(await s.TxHub.GetReceiptsOfExecutablesAsync()).Count;
        }

        internal static void SetBlockVolume(this Svc s, int minimal, int maximal)
        {
            // TODO: Maybe control this in miner
//            s.TxPool.SetBlockVolume(minimal, maximal);
        }

        internal static async Task<byte[]> CallReadOnly(this Svc s, Transaction tx)
        {
            var trace = new TransactionTrace
            {
                TransactionId = tx.GetHash()
            };

            var chainContext = await s.ChainContextService.GetChainContextAsync(Hash.LoadHex(ChainConfig.Instance.ChainId));
            var txCtxt = new TransactionContext
            {
                PreviousBlockHash = chainContext.BlockHash,
                Transaction = tx,
                Trace = trace,
                BlockHeight = chainContext.BlockHeight
            };

            var executive = await s.SmartContractService.GetExecutiveAsync(tx.To, Hash.LoadHex(ChainConfig.Instance.ChainId));

            try
            {
                await executive.SetTransactionContext(txCtxt).Apply();
            }
            finally
            {
                await s.SmartContractService.PutExecutiveAsync(tx.To, executive);
            }

            return trace.RetVal.ToFriendlyBytes();
        }

        internal static MerklePath GetTxRootMerklePathinParentChain(this Svc s, ulong height)
        {
            return s.CrossChainInfo.GetTxRootMerklePathInParentChain(
                s.GetGenesisContractHash(SmartContractType.SideChainContract), height);
        }

        internal static ParentChainBlockInfo GetParentChainBlockInfo(this Svc s, ulong height)
        {
            return s.CrossChainInfo.GetBoundParentChainBlockInfo(
                s.GetGenesisContractHash(SmartContractType.SideChainContract), height);
        }

        internal static ulong GetBoundParentChainHeight(this Svc s, ulong height)
        {
            return s.CrossChainInfo.GetBoundParentChainHeight(
                s.GetGenesisContractHash(SmartContractType.SideChainContract), height);
        }
    }
    
}