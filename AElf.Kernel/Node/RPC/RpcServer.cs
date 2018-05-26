using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using AElf.Node.RPC.DTO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using NLog;

namespace AElf.Kernel.Node.RPC
{
    [LoggerName("RPC")]
    public class RpcServer : IRpcServer
    {
        private const string GetTxMethodName = "get_tx";
        private const string InsertTxMethodName = "insert_tx";
        private const string BroadcastTxMethodName = "broadcast_tx";
        
        /// <summary>
        /// The names of the exposed RPC methods and also the
        /// names used in the JSON to perform a call.
        /// </summary>
        private readonly List<string> _rpcCommands = new List<string>()
        {
            GetTxMethodName,
            InsertTxMethodName,
            BroadcastTxMethodName
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
        /// <returns></returns>
        public bool Start() 
        {
            try
            {
                var host = new WebHostBuilder()
                    .UseKestrel()
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
        /// Verifies the request, especially it checks to see it the command is
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
                       case InsertTxMethodName:
                           responseData = await ProcessInsertTx(reqParams);
                           break;
                       case BroadcastTxMethodName:
                           responseData = await ProcessBroadcastTx(reqParams);
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

        private async Task<JObject> ProcessBroadcastTx(JObject reqParams)
        {
            TransactionDto dto = reqParams["tx"].ToObject<TransactionDto>();

            await _node.BroadcastTransaction(dto.ToTransaction());

            return null;
        }

        private async Task<JObject> ProcessGetTx(JObject reqParams)
        {
            byte[] txid = reqParams["txid"].ToObject<byte[]>();
            ITransaction tx = await _node.GetTransaction(txid);

            if (tx == null)
            {
                // todo tx not found
            }
            
            TransactionDto txDto = tx.ToTransactionDto();
            
            return JObject.FromObject(txDto);
        }
        
        private async Task<JObject> ProcessInsertTx(JObject reqParams)
        {
            TransactionDto dto = reqParams["tx"].ToObject<TransactionDto>();

            IHash txHash = await _node.InsertTransaction(dto.ToTransaction());

            JObject j = new JObject
            {
                ["hash"] = txHash.Value.ToBase64()
            };
            
            return JObject.FromObject(j);
        }

        private async Task WriteResponse(HttpContext context, JObject response)
        {
            if (context?.Response == null)
                return;
            
            await context.Response.WriteAsync(response.ToString(), Encoding.UTF8);
        }
    }
}