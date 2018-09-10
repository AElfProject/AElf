using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.ChainController.EventMessages;
using AElf.ChainController.TxMemPool;
using AElf.Common.ByteArrayHelpers;
using AElf.Common.Extensions;
using AElf.Configuration;
using AElf.Kernel;
using AElf.Kernel.Managers;
using AElf.RPC;
using AElf.SmartContract;
using Community.AspNetCore.JsonRpc;
using Easy.MessageHub;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Google.Protobuf;
using NLog;

namespace AElf.ChainController.Rpc
{
    [Path("/chain")]
    public class ChainControllerRpcService : IJsonRpcService
    {
        #region Properties

        public IChainService ChainService { get; set; }
        public IChainContextService ChainContextService { get; set; }
        public IChainCreationService ChainCreationService { get; set; }
        public ITxPoolService TxPoolService { get; set; }
        public ITransactionManager TransactionManager { get; set; }
        public ITransactionResultService TransactionResultService { get; set; }
        public ISmartContractService SmartContractService { get; set; }
        public IAccountContextService AccountContextService { get; set; }

        #endregion Properties

        private readonly ILogger _logger;

        public ChainControllerRpcService(ILogger logger)
        {
            _logger = logger;
        }
        #region Methods

        [JsonRpcMethod("get_commands")]
        public async Task<JObject> ProcessGetCommands()
        {
            try
            {
                var methodContracts = this.GetRpcMethodContracts();
                var commands = methodContracts.Keys.OrderBy(x => x).ToList();
                var json = JsonConvert.SerializeObject(commands);
                var arrCommands = JArray.Parse(json);
                var response = new JObject
                {
                    ["result"] = new JObject
                    {
                        ["commands"] = arrCommands
                    }
                };
                return await Task.FromResult(JObject.FromObject(response));
            }
            catch (Exception e)
            {
                return new JObject
                {
                    ["error"] = e.ToString()
                };
            }
        }

        [JsonRpcMethod("connect_chain")]
        public async Task<JObject> ProGetChainInfo()
        {
            try
            {
                var chainId = NodeConfig.Instance.ChainId;
                var basicContractZero = this.GetGenesisContractHash(SmartContractType.BasicContractZero);
                var tokenContract = this.GetGenesisContractHash(SmartContractType.TokenContract);
                var response = new JObject()
                {
                    ["result"] =
                        new JObject
                        {
                            [SmartContractType.BasicContractZero.ToString()] = basicContractZero.ToHex(),
                            [SmartContractType.TokenContract.ToString()] = tokenContract.ToHex(),
                            ["chain_id"] = chainId
                        }
                };

                return await Task.FromResult(JObject.FromObject(response));
            }
            catch (Exception e)
            {
                var response = new JObject
                {
                    ["exception"] = e.ToString()
                };

                return await Task.FromResult(JObject.FromObject(response));
            }
        }

        [JsonRpcMethod("get_contract_abi", "address")]
        public async Task<JObject> ProcessGetContractAbi(string address)
        {
            try
            {
                var addrHash = new Hash
                {
                    Value = ByteString.CopyFrom(ByteArrayHelpers.FromHexString(address))
                };

                var abi = await this.GetContractAbi(addrHash);

                return new JObject
                {
                    ["address"] = address,
                    ["abi"] = abi.ToByteArray().ToHex(),
                    ["error"] = ""
                };
            }
            catch (Exception e)
            {
                return new JObject
                {
                    ["address"] = address,
                    ["abi"] = "",
                    ["error"] = "Not Found"
                };
            }
        }

        [JsonRpcMethod("get_increment", "address")]
        public async Task<JObject> ProcessGetIncrementId(string address)
        {
            Hash addr;
            try
            {
                addr = new Hash(ByteArrayHelpers.FromHexString(address));
            }
            catch (Exception e)
            {
                return JObject.FromObject(new JObject
                {
                    ["error"] = "Invalid Address Format"
                });
            }

            var current = await this.GetIncrementId(addr);
            var response = new JObject
            {
                ["result"] = new JObject
                {
                    ["increment"] = current
                }
            };

            return JObject.FromObject(response);
        }

        [JsonRpcMethod("call", "rawtx")]
        public async Task<JObject> ProcessCallReadOnly(string raw64)
        {
            var hexString = ByteArrayHelpers.FromHexString(raw64);
            var transaction = Transaction.Parser.ParseFrom(hexString);

            JObject response;
            try
            {
                var res = await this.CallReadOnly(transaction);
                response = new JObject
                {
                    ["return"] = res.ToHex()
                };
            }
            catch (Exception e)
            {
                response = new JObject
                {
                    ["error"] = e.ToString()
                };
            }

            return JObject.FromObject(response);
        }

        [JsonRpcMethod("broadcast_tx", "rawtx")]
        public async Task<JObject> ProcessBroadcastTx(string raw64)
        {
            var hexString = ByteArrayHelpers.FromHexString(raw64);
            var transaction = Transaction.Parser.ParseFrom(hexString);

            var res = new JObject {["hash"] = transaction.GetHash().ToHex()};
            try
            {
                var valRes = await TxPoolService.AddTxAsync(transaction);
                if (valRes == TxValidation.TxInsertionAndBroadcastingError.Success)
                {
                    MessageHub.Instance.Publish(new TransactionAddedToPool(transaction));
                }
                else
                {
                    res["error"] = valRes.ToString();
                }
            }
            catch (Exception e)
            {
                res["error"] = e.ToString();
            }

            return await Task.FromResult(res);
        }

        [JsonRpcMethod("broadcast_txs", "rawtxs")]
        public async Task<JObject> ProcessBroadcastTxs(string rawtxs)
        {
            var response = new List<object>();

            foreach (var rawtx in rawtxs.Split(','))
            {
                var result = await ProcessBroadcastTx(rawtx);
                if (result.ContainsKey("error"))
                    break;
                response.Add(result["hash"].ToString());
            }

            return new JObject
            {
                ["result"] = JToken.FromObject(response)
            };
        }

        [JsonRpcMethod("get_tx_result", "txhash")]
        public async Task<JObject> ProcGetTxResult(string txhash)
        {
            Hash txHash;
            try
            {
                txHash = ByteArrayHelpers.FromHexString(txhash);
            }
            catch (Exception e)
            {
                return JObject.FromObject(new JObject
                {
                    ["error"] = "Invalid Address Format"
                });
            }

            try
            {
                var transaction = await this.GetTransaction(txHash);

                var txInfo = transaction == null
                    ? new JObject {["tx"] = "Not Found"}
                    : transaction.GetTransactionInfo();
                if (transaction != null)
                    ((JObject) txInfo["tx"]).Add("params",
                        String.Join(", ", await this.GetTransactionParameters(transaction)));

                var txResult = await this.GetTransactionResult(txHash);
                var response = new JObject
                {
                    ["tx_status"] = txResult.Status.ToString(),
                    ["tx_info"] = txInfo["tx"]
                };

                if (txResult.Status == Status.Failed)
                {
                    response["tx_error"] = txResult.RetVal.ToStringUtf8();
                }

                if (txResult.Status == Status.Mined)
                {
                    response["block_number"] = txResult.BlockNumber;
                    response["block_hash"] = txResult.BlockHash.ToHex();
                    response["return"] = txResult.RetVal.ToByteArray().ToHex();
                }
                // Todo: it should be deserialized to obj ion cli, 

                return JObject.FromObject(new JObject {["result"] = response});
            }
            catch (Exception e)
            {
                return new JObject
                {
                    ["error"] = e.ToString()
                };
            }
        }

        [JsonRpcMethod("get_block_height")]
        public async Task<JObject> ProGetBlockHeight()
        {
            var height = await this.GetCurrentChainHeight();
            var response = new JObject
            {
                ["result"] = new JObject
                {
                    ["block_height"] = height.ToString()
                }
            };
            return JObject.FromObject(response);
        }

        [JsonRpcMethod("get_block_info", "block_height", "include_txs")]
        public async Task<JObject> ProGetBlockInfo(string blockHeight, bool includeTxs = false)
        {
            var invalidBlockHeightError = JObject.FromObject(new JObject
            {
                ["error"] = "Invalid Block Height"
            });

            if (!ulong.TryParse(blockHeight, out var height))
            {
                return invalidBlockHeightError;
            }

            var blockinfo = await this.GetBlockAtHeight(height);
            if (blockinfo == null)
                return invalidBlockHeightError;

            var transactionPoolSize = await this.GetTransactionPoolSize();

            // TODO: Create DTO Exntension for Block
            var response = new JObject
            {
                ["result"] = new JObject
                {
                    ["Blockhash"] = blockinfo.GetHash().ToHex(),
                    ["Header"] = new JObject
                    {
                        ["PreviousBlockHash"] = blockinfo.Header.PreviousBlockHash.ToHex(),
                        ["MerkleTreeRootOfTransactions"] = blockinfo.Header.MerkleTreeRootOfTransactions.ToHex(),
                        ["MerkleTreeRootOfWorldState"] = blockinfo.Header.MerkleTreeRootOfWorldState.ToHex(),
                        ["Index"] = blockinfo.Header.Index.ToString(),
                        ["Time"] = blockinfo.Header.Time.ToDateTime(),
                        ["ChainId"] = blockinfo.Header.ChainId.ToHex()
                    },
                    ["Body"] = new JObject
                    {
                        ["TransactionsCount"] = blockinfo.Body.TransactionsCount
                    },
                    ["CurrentTransactionPoolSize"] = transactionPoolSize
                }
            };

            if (includeTxs)
            {
                var transactions = blockinfo.Body.Transactions;
                var txs = new List<string>();
                foreach (var txHash in transactions)
                {
                    txs.Add(txHash.ToHex());
                }

                response["result"]["Body"]["Transactions"] = JArray.FromObject(txs);
            }

            return JObject.FromObject(response);
        }

        #region Admin

        [JsonRpcMethod("set_block_volume", "minimal", "maximal")]
        public async Task<JObject> ProcSetBlockVolume(string minimal, string maximal)
        {
            /* TODO: This is a privileged method, need:
             *   1. Optional enabling of this method (maybe separate endpoint), and/or
             *   2. Authentication / authorization
             */
            try
            {
                var min = ulong.Parse(minimal);
                var max = ulong.Parse(maximal);
                this.SetBlockVolume(min, max);
                return await Task.FromResult(new JObject
                {
                    ["result"] = "Success"
                });
            }
            catch (Exception e)
            {
                _logger.Error("ProcSetBlockVolume failed: " + e);
                return await Task.FromResult(new JObject
                {
                    ["error"] = "Failed"
                });
            }
        }

        #endregion Admin

        #endregion Methods
    }
}