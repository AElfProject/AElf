using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.ChainController.EventMessages;
using AElf.Configuration;
using AElf.Kernel;
using AElf.Common;
using AElf.Database;
using AElf.Kernel.Managers;
using AElf.Kernel.Types;
using AElf.Miner.EventMessages;
using AElf.Miner.TxMemPool;
using AElf.Node.AElfChain;
using AElf.Node.CrossChain;
using AElf.RPC;
using AElf.SmartContract;
using Community.AspNetCore.JsonRpc;
using Easy.MessageHub;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Google.Protobuf;
using Newtonsoft.Json.Serialization;
using NLog;
using NServiceKit.Text;
using ServiceStack.Templates;

namespace AElf.ChainController.Rpc
{
    [Path("/chain")]
    public class ChainControllerRpcService : IJsonRpcService
    {
        #region Properties

        public IChainService ChainService { get; set; }
        public IChainContextService ChainContextService { get; set; }
        public IChainCreationService ChainCreationService { get; set; }
        public ITxHub TxHub { get; set; }
        public ITransactionResultService TransactionResultService { get; set; }
        public ITransactionTraceManager TransactionTraceManager { get; set; }
        public ISmartContractService SmartContractService { get; set; }
        public INodeService MainchainNodeService { get; set; }
        public ICrossChainInfo CrossChainInfo { get; set; }
        public IKeyValueDatabase KeyValueDatabase { get; set; }

        #endregion Properties

        private readonly ILogger _logger;

        private bool _canBroadcastTxs = true;

        public ChainControllerRpcService(ILogger logger)
        {
            _logger = logger;

            MessageHub.Instance.Subscribe<ReceivingHistoryBlocksChanged>(msg => _canBroadcastTxs = !msg.IsReceiving);
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
                var sideChainContract = this.GetGenesisContractHash(SmartContractType.SideChainContract);
                //var tokenContract = this.GetGenesisContractHash(SmartContractType.TokenContract);
                var response = new JObject()
                {
                    ["result"] =
                        new JObject
                        {
                            [SmartContractType.BasicContractZero.ToString()] = basicContractZero.DumpHex(),
                            [SmartContractType.SideChainContract.ToString()] = sideChainContract.DumpHex(),
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
                var addrHash =Address.LoadHex(address);

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
            Address addr;
            try
            {
                addr = Address.LoadHex(address);
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

            var res = new JObject {["hash"] = transaction.GetHash().DumpHex()};

            if (!_canBroadcastTxs)
            {
                res["error"] = "Sync still in progress, cannot send transactions.";
                return await Task.FromResult(res);
            }
            
//            try
//            {
            // TODO: Wait validation done
                await TxHub.AddTransactionAsync(transaction);
//                if (valRes == TxValidation.TxInsertionAndBroadcastingError.Success)
//                {
//                    MessageHub.Instance.Publish(new TransactionAddedToPool(transaction));
//                }
//                else
//                {
//                    res["error"] = valRes.ToString();
//                }
//            }
//            catch (Exception e)
//            {
//                res["error"] = e.ToString();
//            }

            return await Task.FromResult(res);
        }

        [JsonRpcMethod("broadcast_txs", "rawtxs")]
        public async Task<JObject> ProcessBroadcastTxs(string rawtxs)
        {
            var response = new List<object>();
            
            if (!_canBroadcastTxs)
            {
                return new JObject
                {
                    ["result"] = JToken.FromObject(string.Empty),
                    ["error"] = "Sync still in progress, cannot send transactions."
                };
            }

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

        [JsonRpcMethod("get_merkle_path", "txid")]
        public async Task<JObject> ProcGetTxMerklePath(string txid)
        {
            try
            {
                Hash txHash;
                try
                {
                    txHash = Hash.LoadHex(txid);
                }
                catch (Exception)
                {
                    throw new Exception("Invalid Address Format");
                }
                var txResult = await this.GetTransactionResult(txHash);
                if(txResult.Status != Status.Mined)
                   throw new Exception("Transaction is not mined.");
                
                var merklePath = txResult.MerklePath?.Clone();
                if(merklePath == null)
                    throw new Exception("Not found merkle path for this transaction.");
                MerklePath merklePathInParentChain = null;
                ulong boundParentChainHeight = 0;
                try
                {
                    merklePathInParentChain = this.GetTxRootMerklePathinParentChain(txResult.BlockNumber);
                    boundParentChainHeight = this.GetBoundParentChainHeight(txResult.BlockNumber);
                }
                catch (Exception e)
                {
                    throw new Exception("Unable to get merkle path from parent chain");
                }
                /*if(merklePathInParentChain == null)
                    throw new Exception("Not found merkle path in parent chain");*/
                if(merklePathInParentChain != null)
                    merklePath.Path.AddRange(merklePathInParentChain.Path);
                return new JObject
                {
                    ["merkle_path"] = merklePath.ToByteArray().ToHex(),
                    ["parent_height"] = boundParentChainHeight
                };
            }
            catch (Exception e)
            {
                return new JObject
                {
                    ["error"] = e.Message
                };
            }
        }
        
        [JsonRpcMethod("get_pcb_info", "height")]
        public async Task<JObject> ProcGetPCB(string height)
        {
            try
            {
                ulong h;
                try
                {
                    h = ulong.Parse(height);
                }
                catch (Exception)
                {
                    throw new Exception("Invalid height");
                }
                var merklePathInParentChain = this.GetParentChainBlockInfo(h);
                if (merklePathInParentChain == null)
                {
                    throw new Exception("Unable to get parent chain block at height " + height);
                }
                return new JObject
                {
                    ["parent_chainId"] = merklePathInParentChain.Root.ChainId.DumpHex(),
                    ["side_chain_txs_root"] = merklePathInParentChain.Root.SideChainTransactionsRoot.DumpHex(),
                    ["parent_height"] = merklePathInParentChain.Height
                };
            }
            catch (Exception e)
            {
                return new JObject
                {
                    ["error"] = e.Message
                };
            }
        }
        
        [JsonRpcMethod("get_tx_result", "txhash")]
        public async Task<JObject> ProcGetTxResult(string txhash)
        {
            Hash txHash;
            try
            {
                txHash = Hash.LoadHex(txhash);
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
#if DEBUG
                var txtrc = await this.GetTransactionTrace(txHash, txResult.BlockNumber);
                response["tx_trc"] = txtrc?.ToString();
#endif
                if (txResult.Status == Status.Failed)
                {
                    response["tx_error"] = txResult.RetVal.ToStringUtf8();
                }

                if (txResult.Status == Status.Mined)
                {
                    response["block_number"] = txResult.BlockNumber;
                    response["block_hash"] = txResult.BlockHash.DumpHex();
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
                    ["Blockhash"] = blockinfo.GetHash().DumpHex(),
                    ["Header"] = new JObject
                    {
                        ["PreviousBlockHash"] = blockinfo.Header.PreviousBlockHash.DumpHex(),
                        ["MerkleTreeRootOfTransactions"] = blockinfo.Header.MerkleTreeRootOfTransactions.DumpHex(),
                        ["MerkleTreeRootOfWorldState"] = blockinfo.Header.MerkleTreeRootOfWorldState.DumpHex(),
                        ["SideChainTransactionsRoot"] = blockinfo.Header.SideChainTransactionsRoot.DumpHex(),
                        ["Index"] = blockinfo.Header.Index.ToString(),
                        ["Time"] = blockinfo.Header.Time.ToDateTime(),
                        ["ChainId"] = blockinfo.Header.ChainId.DumpHex(),
                        //["IndexedInfo"] = blockinfo.Header.GetIndexedSideChainBlcokInfo()
                    },
                    ["Body"] = new JObject
                    {
                        ["TransactionsCount"] = blockinfo.Body.TransactionsCount,
                        ["IndexedSideChainBlcokInfo"] = blockinfo.GetIndexedSideChainBlockInfo()
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
                    txs.Add(txHash.DumpHex());
                }

                response["result"]["Body"]["Transactions"] = JArray.FromObject(txs);
            }

            return JObject.FromObject(response);
        }

        [JsonRpcMethod("get_txpool_size")]
        public async Task<JObject> ProGetTxPoolSize()
        {
            var transactionPoolSize = await this.GetTransactionPoolSize();
            var response = new JObject
            {
                ["CurrentTransactionPoolSize"] = transactionPoolSize
            };

            return JObject.FromObject(response);
        }
        
        [JsonRpcMethod("dpos_isalive")]
        public async Task<JObject> ProIsDPoSAlive()
        {
            var isAlive = MainchainNodeService.IsDPoSAlive();
            var response = new JObject
            {
                ["IsAlive"] = isAlive
            };

            return JObject.FromObject(response);
        }
        
        [JsonRpcMethod("node_isforked")]
        public async Task<JObject> ProNodeIsForked()
        {
            var isForked = MainchainNodeService.IsForked();
            var response = new JObject
            {
                ["IsForked"] = isForked
            };

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
                var min = int.Parse(minimal);
                var max = int.Parse(maximal);
                this.SetBlockVolume(min, max);
                return await Task.FromResult(new JObject
                {
                    ["result"] = "Success"
                });
            }
            catch (Exception e)
            {
                _logger.Error(e, "Exception while ProcSetBlockVolume.");
                return await Task.FromResult(new JObject
                {
                    ["error"] = "Failed"
                });
            }
        }

        #endregion Admin
        
        [JsonRpcMethod("get_db_value","key")]
        public async Task<JObject> GetDbValue(string key)
        {
            string type = string.Empty;
            JToken id;
            try
            {
                var valueBytes = KeyValueDatabase.GetAsync(key).Result;

                object value;

                if (key.StartsWith(GlobalConfig.StatePrefix))
                {
                    type = "State";
                    id = key.Substring(GlobalConfig.StatePrefix.Length, key.Length - GlobalConfig.StatePrefix.Length);
                    value = StateValue.Create(valueBytes);
                }
                else if(key.StartsWith(GlobalConfig.TransactionReceiptPrefix))
                {
                    type = "TransactionReceipt";
                    id = key.Substring(GlobalConfig.TransactionReceiptPrefix.Length, key.Length - GlobalConfig.TransactionReceiptPrefix.Length);
                    value = valueBytes?.Deserialize<TransactionReceipt>();
                }
                else
                {
                    var keyObj = Key.Parser.ParseFrom(ByteArrayHelpers.FromHexString(key));
                    type = keyObj.Type;
                    id = JObject.Parse(keyObj.ToString());
                    var obj = GetInstance(type);
                    obj.MergeFrom(valueBytes);
                    value = obj;
                }

                var response = new JObject
                {
                    ["Type"] = type,
                    ["Id"] = id,
                    ["Value"] = JObject.Parse(value.ToString())
                };

                return JObject.FromObject(response);
            }
            catch (Exception e)
            {
                var response = new JObject
                {
                    ["Type"]=type,
                    ["Value"] = e.Message
                };
                return JObject.FromObject(response);
            }
        }

        private IMessage GetInstance(string type)
        {
            switch (type)
            {
                case "MerklePath":
                    return new MerklePath();
                case "BinaryMerkleTree":
                    return new BinaryMerkleTree ();
                case "BlockHeader":
                    return new BlockHeader();
                case "BlockBody":
                    return new BlockBody();
                case "Hash":
                    return new Hash();
                case "SmartContractRegistration":
                    return new SmartContractRegistration();
                case "Transaction":
                    return new Transaction();
                case "TransactionResult":
                    return new TransactionResult();
                case "TransactionTrace":
                    return new TransactionTrace();
                default:
                    throw new ArgumentException($"[{type}] not found");
            }
        }

        #endregion Methods
    }
}