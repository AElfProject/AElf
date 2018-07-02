﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AElf.Common.Attributes;
using AElf.Common.ByteArrayHelpers;
using AElf.Kernel.Node.RPC.DTO;
using AElf.Network.Data;
using AElf.Node.RPC.DTO;
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
        private const string GetTxMethodName = "get_tx";
        //private const string InsertTxMethodName = "insert_tx";
        private const string BroadcastTxMethodName = "broadcast_tx";
        //private const string GetPeersMethodName = "get_peers";
        private const string GetIncrementIdMethodName = "get_increment";
        //private const string BroadcastBlockMethodName = "broadcast_block";
        private const string GetTxResultMethodName = "get_tx_result";
        private const string GetCommandsMethodName = "get_commands";
        private const string GetContractAbi = "get_contract_abi";
        private const string GetGenesisiAddress = "connect_chain";
        private const string GetDeserializedData = "get_deserialized_result";

        /// <summary>
        /// The names of the exposed RPC methods and also the
        /// names used in the JSON to perform a call.
        /// </summary>
        private readonly List<string> _rpcCommands = new List<string>()
        {
            GetTxMethodName,
            //InsertTxMethodName,
            BroadcastTxMethodName,
            //GetPeersMethodName,
            GetCommandsMethodName,
            GetIncrementIdMethodName,
            //BroadcastBlockMethodName,
            GetContractAbi,
            GetTxResultMethodName,
            GetGenesisiAddress,
            GetDeserializedData
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
        public bool Start(int rpcPort)
        {
            try
            {
                var host = new WebHostBuilder()
                    .UseKestrel()
                    .UseUrls("http://localhost:" + rpcPort)
                    .ConfigureLogging((hostingContext, logging) =>
                    {
                        //logging.ClearProviders(); 
                    })
                    .Configure(a => a.Run(ProcessAsync))
                    .Build();

                host.RunAsync();
            }
            catch (Exception e)
            {
                _logger.LogException(LogLevel.Error, "Error while starting the RPC server.", e);
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
                string bodyAsString = null;
                using (var streamReader = new StreamReader(context.Request.Body, Encoding.UTF8))
                {
                    bodyAsString = streamReader.ReadToEnd();
                }

                JObject req = JObject.Parse(bodyAsString);

                return req;
            }
            catch (Exception e)
            {
                _logger.LogException(LogLevel.Error, "Error while parsing the RPC request.", e);
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

            JToken method = JToken.FromObject(request["method"]);

            if (method != null)
            {
                string methodName = method.ToObject<string>();
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

            JObject request = ParseRequest(context);

            if (request == null)
            {
                JObject err = ErrorResponseFactory.GetParseError(0);
                await WriteResponse(context, err);
                return;
            }

            JObject validErr = ValidateRequest(request);

            if (validErr != null)
            {
                await WriteResponse(context, validErr);
                return;
            }

            try
            {
                // read id
                int reqId = request["id"].ToObject<int>();

                string methodName = JToken.FromObject(request["method"]).ToObject<string>();
                JObject reqParams = JObject.FromObject(request["params"]);

                JObject responseData = null;
                switch (methodName)
                {
                    case GetTxMethodName:
                        responseData = await ProcessGetTx(reqParams);
                        break;
                    /*case InsertTxMethodName:
                        responseData = await ProcessInsertTx(reqParams);
                        break;*/
                    case BroadcastTxMethodName:
                        responseData = await ProcessBroadcastTx(reqParams);
                        break;
                    /*case GetPeersMethodName:
                        responseData = await ProcessGetPeers(reqParams);
                        break;*/
                    case GetCommandsMethodName:
                        responseData = ProcessGetCommands();
                        break;
                    case GetIncrementIdMethodName:
                        responseData = await ProcessGetIncrementId(reqParams);
                        break;
                    /*case BroadcastBlockMethodName:
                        responseData = await ProcessBroadcastBlock(reqParams);
                        break;*/
                    case GetContractAbi:
                        responseData = await ProcessGetContractAbi(reqParams);
                        break;
                    case GetTxResultMethodName:
                        responseData = await ProcGetTxResult(reqParams);
                        break;
                    case GetGenesisiAddress:
                        responseData = await ProGetGenesisAddress(reqParams);
                        break;
                    default:
                        Console.WriteLine("Method name not found"); // todo log
                        break;
                }

                if (responseData == null)
                {
                    // todo write error 
                }

                JObject resp = JsonRpcHelpers.CreateResponse(responseData, reqId);

                await WriteResponse(context, resp);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private Task<JObject> ProGetGenesisAddress(JObject reqParams)
        {
            var genesisHash = _node.GetGenesisContractHash();
            Hash chainId = _node.ChainId;  
            JObject j = new JObject
            {
                ["result"] = new JObject
                {
                    ["genesis-contract"] = genesisHash.Value.ToByteArray().ToHex(),
                    ["chain-id"] = chainId.Value.ToByteArray().ToHex()
                }
            };
            
            return Task.FromResult(JObject.FromObject(j));
        }

        private async Task<JObject> ProcGetTxResult(JObject reqParams)
        {
            string adr = reqParams["txhash"].ToString();
            Hash txHash;
            
            try
            {
                txHash = ByteArrayHelpers.FromHexString(adr);
            }
            catch (Exception e)
            {
                return JObject.FromObject(new JObject
                {
                    ["error"] = "Error: Invalid Input Format"
                });
            }
            
            TransactionResult txResult = await _node.GetTransactionResult(txHash);
            var jobj = new JObject
            {
                ["tx_id"] = txResult.TransactionId.Value.ToByteArray().ToHex(),
                ["tx_status"] = txResult.Status.ToString()
            };

            // for the case that return type is Hash
            
            if (txResult.Status == Status.Failed)
            {
                jobj["tx_error"] = txResult.RetVal.ToStringUtf8();
            }

            if (txResult.Status == Status.Mined)
            {
                jobj["return"] = txResult.RetVal.ToByteArray().ToHex();
            }
            // Todo: it should be deserialized to obj ion cli, 
            
            
            
            JObject j = new JObject
            {
                ["result"] = jobj
            };
            
            return JObject.FromObject(j);
        }

        private async Task<JObject> ProcessGetIncrementId(JObject reqParams)
        {
            string adr = reqParams["address"].ToString();
            
            Hash addr;
            try
            {
                addr = new Hash(ByteArrayHelpers.FromHexString(adr));
            }
            catch (Exception e)
            {
                return JObject.FromObject(new JObject
                {
                    ["error"] = "Error: Invalid Input Format"
                });
            }
            
            ulong current = await _node.GetIncrementId(addr);

            JObject j = new JObject
            {
                ["result"] = new JObject
                {
                    ["increment"] = current
                }
            };

            return JObject.FromObject(j);
        }

        private async Task<JObject> ProcessGetContractAbi(JObject reqParams)
        {
            string addr = reqParams["address"] == null
                ? _node.GetGenesisContractHash().Value.ToByteArray().ToHex()
                : reqParams["address"].ToString();
            Hash addrHash = new Hash()
            {
                Value = ByteString.CopyFrom(ByteArrayHelpers.FromHexString(addr))
            };

            JObject j = null;

            try
            {
                var abi = await _node.GetContractAbi(addrHash);
                j = new JObject
                {
                    ["address"] = addr,
                    ["abi"] = abi.ToByteArray().ToHex(),
                    ["error"] = ""
                };
            }
            catch (ArgumentNullException e)
            {
                j = new JObject
                {
                    ["address"] = addr,
                    ["abi"] = "",
                    ["error"] = "Not Found"
                };
            }

            return j;
        }

        private async Task<JObject> ProcessBroadcastTx(JObject reqParams)
        {
            string raw64 = reqParams["rawtx"].ToString();

            byte[] b = ByteArrayHelpers.FromHexString(raw64);
            Transaction t = Transaction.Parser.ParseFrom(b);

            var res = await _node.BroadcastTransaction(t);

            JObject j;
            if (!res)
            {
                j = new JObject
                {
                    ["error"] = "Invalid transaction"
                };
                return JObject.FromObject(j);
            }

            j = new JObject { ["hash"] = t.GetHash().Value.ToByteArray().ToHex() };
            
            return JObject.FromObject(j);
        }

        /// <summary>
        /// This method processes the request for a specified
        /// number of transactions
        /// </summary>
        /// <param name="reqParams"></param>
        /// <returns></returns>
        private async Task<JObject> ProcessGetTx(JObject reqParams)
        {
            byte[] txid = reqParams["txid"].ToObject<byte[]>();
            ITransaction tx = await _node.GetTransaction(txid);

            var txInfo = tx == null ? new JObject {["tx"] = "Not Found"} : tx.GetTransactionInfo();

            return txInfo;
        }

        /// <summary>
        /// This method returns the list of all RPC commands
        /// except "get_commands"
        /// </summary>
        /// <returns></returns>
        private JObject ProcessGetCommands()
        {
            List<string> commands = _rpcCommands.Where(x => x != GetCommandsMethodName).ToList();
            var json = JsonConvert.SerializeObject(commands);
            JArray arrCommands = JArray.Parse(json);

            JObject j = new JObject()
            {
                ["result"] = new JObject
                    {
                        ["commands"] = arrCommands
                    }
            };

            return JObject.FromObject(j);
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