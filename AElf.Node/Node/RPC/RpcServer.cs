using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.Common.Attributes;
using AElf.Common.ByteArrayHelpers;
using AElf.Kernel.Node.RPC.DTO;
using Google.Protobuf;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace AElf.Kernel.Node.RPC
{
    [LoggerName("RPC")]
    public class RpcServer : IRpcServer
    {
        //private const string GetTxMethodName = "get_tx";
        //private const string InsertTxMethodName = "insert_tx";
        private const string BroadcastTxMethodName = "broadcast_tx";
        private const string BroadcastTxsMethodName = "broadcast_txs";

        //private const string GetPeersMethodName = "get_peers";
        private const string GetIncrementIdMethodName = "get_increment";

        //private const string BroadcastBlockMethodName = "broadcast_block";
        private const string GetTxResultMethodName = "get_tx_result";
        private const string GetCommandsMethodName = "get_commands";
        private const string GetContractAbi = "get_contract_abi";
        private const string GetGenesisiAddress = "connect_chain";
        private const string GetDeserializedData = "get_deserialized_result";
        private const string GetBlockHeight = "get_block_height";
        private const string GetBlockInfo = "get_block_info";
        private const string GetDeserializedInfo = "get_deserialized_info";
        private const string SetBlockVolume = "set_block_volume";
        private const string CallReadOnly = "call";

        /// <summary>
        /// The names of the exposed RPC methods and also the
        /// names used in the JSON to perform a call.
        /// </summary>
        private readonly List<string> _rpcCommands = new List<string>
        {
            BroadcastTxMethodName,
            BroadcastTxsMethodName,
            GetCommandsMethodName,
            GetIncrementIdMethodName,
            GetContractAbi,
            GetTxResultMethodName,
            GetGenesisiAddress,
            GetDeserializedData,
            GetBlockHeight,
            GetBlockInfo,
            GetDeserializedInfo,
            CallReadOnly,
            SetBlockVolume
        };

        /// <summary>
        /// Represents the node itself.
        /// </summary>
        private MainChainNode _node;

        private readonly ILogger _logger;

        public RpcServer(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Temporary solution, this is used for injecting a
        /// reference to the node.
        /// todo : remove dependency on the node
        /// </summary>
        /// <param name="node"></param>
        public void SetCommandContext(MainChainNode node)
        {
            _node = node;
        }

        /// <summary>
        /// Starts the Kestrel server.
        /// </summary>
        /// <param name="rpcPort"></param>
        /// <returns></returns>
        public bool Start(string rpcHost, int rpcPort)
        {
            try
            {
                var url = "http://" + rpcHost + ":" + rpcPort;
                var host = new WebHostBuilder()
                    .UseKestrel()
                    .UseUrls(url)
                    .ConfigureLogging((hostingContext, logging) => { })
                    .Configure(a => a.Run(ProcessAsync))
                    .Build();

                host.RunAsync();
            }
            catch (Exception e)
            {
                _logger.Log(LogLevel.Error, e, "Error while starting the RPC server.");
                return false;
            }

            return true;
        }

        private JObject ParseRequest(HttpContext context)
        {
            if (context?.Request?.Body == null)
                return null;

            try
            {
                string bodyAsString;
                using (var streamReader = new StreamReader(context.Request.Body, Encoding.UTF8))
                {
                    bodyAsString = streamReader.ReadToEnd();
                }

                return JObject.Parse(bodyAsString);
            }
            catch (Exception e)
            {
                _logger.Log(LogLevel.Error, e, "Error while parsing the RPC request.");
                return null;
            }
        }

        /// <summary>
        /// Verifies the request, it especially checks to see if the command is
        /// registered.
        /// </summary>
        /// <param name="request">The request to verify</param>
        /// <returns>Null if the request is valid, the response if verification fails</returns>
        private JObject ValidateRequest(JObject request)
        {
            if (request == null)
                return null;

            var method = JToken.FromObject(request["method"]);
            if (method != null)
            {
                var methodName = method.ToObject<string>();
                if (string.IsNullOrEmpty(methodName) || !_rpcCommands.Contains(methodName))
                {
                    return ErrorResponseFactory.GetMethodNotFound(request["id"].ToObject<int>());
                }
            }

            return null;
        }

        /// <summary>
        /// Callback that setup to process the requests : parse, validate and dispatch
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private async Task ProcessAsync(HttpContext context)
        {
            if (context?.Request?.Body == null)
                return;

            var request = ParseRequest(context);
            if (request == null)
            {
                var err = ErrorResponseFactory.GetParseError(0);
                await WriteResponse(context, err);
                return;
            }

            var validErr = ValidateRequest(request);
            if (validErr != null)
            {
                await WriteResponse(context, validErr);
                return;
            }

            try
            {
                var reqId = request["id"].ToObject<int>();
                var methodName = JToken.FromObject(request["method"]).ToObject<string>();
                var reqParams = JObject.FromObject(request["params"]);

                JObject response = null;
                switch (methodName)
                {
                    /*case GetTxMethodName:
                        response = await ProcessGetTx(reqParams);
                        break;*/
                    /*case InsertTxMethodName:
                        response = await ProcessInsertTx(reqParams);
                        break;*/
                    case BroadcastTxMethodName:
                        response = await ProcessBroadcastTx(reqParams);
                        break;
                    case BroadcastTxsMethodName:
                        response = await ProcessBroadcastTxs(reqParams);
                        break;
                    /*case GetPeersMethodName:
                        responseData = await ProcessGetPeers(reqParams);
                        break;*/
                    case GetCommandsMethodName:
                        response = ProcessGetCommands();
                        break;
                    case GetIncrementIdMethodName:
                        response = await ProcessGetIncrementId(reqParams);
                        break;
                    /*case BroadcastBlockMethodName:
                        responseData = await ProcessBroadcastBlock(reqParams);
                        break;*/
                    case GetContractAbi:
                        response = await ProcessGetContractAbi(reqParams);
                        break;
                    case GetTxResultMethodName:
                        response = await ProcGetTxResult(reqParams);
                        break;
                    case GetGenesisiAddress:
                        response = await ProGetGenesisAddress(reqParams);
                        break;
                    case GetBlockHeight:
                        response = await ProGetBlockHeight(reqParams);
                        break;
                    case GetBlockInfo:
                        response = await ProGetBlockInfo(reqParams);
                        break;
                    case GetDeserializedInfo:
                        response = ProGetDeserializedInfo(reqParams);
                        break;
                    case CallReadOnly:
                        response = await ProcessCallReadOnly(reqParams);
                        break;
                    case SetBlockVolume:
                        response = ProcSetBlockVolume(reqParams);
                        break;
                    default:
                        Console.WriteLine("Method name not found"); // todo log
                        break;
                }

                if (response == null)
                {
                    // todo write error 
                }

                var resp = JsonRpcHelpers.CreateResponse(response, reqId);
                await WriteResponse(context, resp);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private JObject ProcSetBlockVolume(JObject reqParams)
        {
            try
            {
                var min = ulong.Parse(reqParams["minimal"].ToString());
                var max = ulong.Parse(reqParams["maximal"].ToString());
                _node.SetBlockVolume(min, max);
                return new JObject
                {
                    ["result"] = "Success"
                };
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return new JObject
                {
                    ["error"] = "Failed"
                };
            }
        }

        private JObject ProGetDeserializedInfo(JObject reqParams)
        {
            try
            {
                var sKey = reqParams["key"].ToString();
                var byteKey = ByteArrayHelpers.FromHexString(sKey);
                var key = Key.Parser.ParseFrom(byteKey);
                var keyType = key.Type;

                return new JObject
                {
                    ["type"] = keyType
                };
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return new JObject
                {
                    ["error"] = "Unknown key"
                };
            }
        }

        private async Task<JObject> ProGetBlockInfo(JObject reqParams)
        {
            var error = JObject.FromObject(new JObject
            {
                ["error"] = "Invalid Block Height"
            });

            var height = int.Parse(reqParams["block_height"].ToString());
            if (height < 0)
                return error;

            var blockinfo = await _node.GetBlockAtHeight(height);
            if (blockinfo == null)
                return error;

            var transactionPoolSize = await _node.GetTransactionPoolSize();

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
           
            if (reqParams["include_txs"] != null && reqParams["include_txs"].Value<Boolean>())
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

        private async Task<JObject> ProGetBlockHeight(JObject reqParams)
        {
            var height = await _node.GetCurrentChainHeight();
            var response = new JObject
            {
                ["result"] = new JObject
                {
                    ["block_height"] = height.ToString()
                }
            };
            return JObject.FromObject(response);
        }


        private Task<JObject> ProGetGenesisAddress(JObject reqParams)
        {
            var chainId = _node.ChainId;
            var basicContractZero = _node.GetGenesisContractHash(SmartContractType.BasicContractZero);
            var tokenContract = _node.GetGenesisContractHash(SmartContractType.TokenContract);
            var response = new JObject
            {
                ["result"] = new JObject
                {
                    [SmartContractType.BasicContractZero.ToString()] = basicContractZero.ToHex(),
                    [SmartContractType.TokenContract.ToString()] = tokenContract.ToHex(),
                    ["chain_id"] = chainId.ToHex()
                }
            };

            return Task.FromResult(JObject.FromObject(response));
        }

        private async Task<JObject> ProcGetTxResult(JObject reqParams)
        {
            Hash txHash;
            try
            {
                txHash = ByteArrayHelpers.FromHexString(reqParams["txhash"].ToString());
            }
            catch (Exception e)
            {
                return JObject.FromObject(new JObject
                {
                    ["error"] = "Invalid Address Format"
                });
            }

            var transaction = await _node.GetTransaction(txHash);

            var txInfo = transaction == null ? new JObject {["tx"] = "Not Found"} : transaction.GetTransactionInfo();

            var txResult = await _node.GetTransactionResult(txHash);
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
                response["return"] = txResult.RetVal.ToByteArray().ToHex();
            }
            // Todo: it should be deserialized to obj ion cli, 

            return JObject.FromObject(new JObject {["result"] = response});
        }

        private async Task<JObject> ProcessGetIncrementId(JObject reqParams)
        {
            Hash addr;
            try
            {
                addr = new Hash(ByteArrayHelpers.FromHexString(reqParams["address"].ToString()));
            }
            catch (Exception e)
            {
                return JObject.FromObject(new JObject
                {
                    ["error"] = "Invalid Address Format"
                });
            }

            var current = await _node.GetIncrementId(addr);
            var response = new JObject
            {
                ["result"] = new JObject
                {
                    ["increment"] = current
                }
            };

            return JObject.FromObject(response);
        }

        private async Task<JObject> ProcessGetContractAbi(JObject reqParams)
        {
            var addr = reqParams["address"]?.ToString();
            
            
            JObject response;
            try
            {
                var addrHash = new Hash
                {
                    Value = ByteString.CopyFrom(ByteArrayHelpers.FromHexString(addr))
                };

                IMessage abi;
                if (reqParams["name"] != null)
                    abi = await _node.GetContractAbi(addrHash, reqParams["name"].ToString());
                else
                {
                    abi = await _node.GetContractAbi(addrHash);
                }

                response = new JObject
                {
                    ["address"] = addr,
                    ["abi"] = abi.ToByteArray().ToHex(),
                    ["error"] = ""
                };
            }
            catch (Exception e)
            {
                response = new JObject
                {
                    ["address"] = addr,
                    ["abi"] = "",
                    ["error"] = "Not Found"
                };
            }

            return response;
        }

        private async Task<JObject> ProcessBroadcastTx(JObject reqParams)
        {
            var raw64 = reqParams["rawtx"].ToString();
            var hexString = ByteArrayHelpers.FromHexString(raw64);
            var transaction = Transaction.Parser.ParseFrom(hexString);

            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var res = await _node.BroadcastTransaction(transaction);
            stopWatch.Stop();
            //_logger?.Info($"### Debug ProcessBroadcastTx Time: {stopWatch.ElapsedMilliseconds}");

            JObject response;
            if (res != TxValidation.TxInsertionAndBroadcastingError.Success)
            {
                response = new JObject
                {
                    ["error"] = res.ToString()
                };
                return JObject.FromObject(response);
            }

            response = new JObject {["hash"] = transaction.GetHash().ToHex()};
            return JObject.FromObject(response);
        }

        private async Task<JObject> ProcessBroadcastTxs(JObject reqParams)
        {
            var response = new List<object>();
            int count = 0;
            foreach (var rawtx in reqParams["rawtxs"].ToString().Split(','))
            {
                var result = await ProcessBroadcastTx(new JObject {["rawtx"] = rawtx});
                if (result.ContainsKey("error"))
                    break;
                response.Add(result["hash"].ToString());
                count++;
            }
            _logger.Log(LogLevel.Info, "Batch request pass count number:" + count.ToString());
            
            return new JObject
            {
                ["result"] = JToken.FromObject(response)
            };
        }

        private async Task<JObject> ProcessCallReadOnly(JObject reqParams)
        {
            var raw64 = reqParams["rawtx"].ToString();
            var hexString = ByteArrayHelpers.FromHexString(raw64);
            var transaction = Transaction.Parser.ParseFrom(hexString);

            JObject response;
            try
            {
                var res = await _node.CallReadOnly(transaction);
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

        /// <summary>
        /// This method processes the request for a specified
        /// number of transactions
        /// </summary>
        /// <param name="reqParams"></param>
        /// <returns></returns>
        private async Task<JObject> ProcessGetTx(JObject reqParams)
        {
            var txid = reqParams["txid"].ToObject<byte[]>();
            var transaction = await _node.GetTransaction(txid);

            var txInfo = transaction == null ? new JObject {["tx"] = "Not Found"} : transaction.GetTransactionInfo();

            return txInfo;
        }

        /// <summary>
        /// This method returns the list of all RPC commands
        /// except "get_commands"
        /// </summary>
        /// <returns></returns>
        private JObject ProcessGetCommands()
        {
            var commands = _rpcCommands.Where(x => x != GetCommandsMethodName).ToList();
            var json = JsonConvert.SerializeObject(commands);
            var arrCommands = JArray.Parse(json);

            var response = new JObject
            {
                ["result"] = new JObject
                {
                    ["commands"] = arrCommands
                }
            };

            return JObject.FromObject(response);
        }

        private async Task<JObject> ProcessBroadcastBlock(JObject reqParams)
        {
            throw new NotImplementedException();
        }

        private async Task WriteResponse(HttpContext context, JObject response)
        {
            if (context?.Response == null)
                return;

            await context.Response.WriteAsync(response.ToString(), Encoding.UTF8);
        }
    }
}