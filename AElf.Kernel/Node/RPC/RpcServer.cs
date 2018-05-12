using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using AElf.Node.RPC.DTO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace AElf.Kernel.Node.RPC
{
    public class RpcServer : IRpcServer
    {
        private const string GetTxMethodName = "get_tx";
        private const string InsertTxMethodName = "insert_tx";
        
        private readonly List<string> _rpcCommands = new List<string>()
        {
            GetTxMethodName,
            InsertTxMethodName
        };
        
        private MainChainNode _node;
        
        public RpcServer()
        {
        }

        public void SetCommandContext(MainChainNode node)
        {
            _node = node;
        }
        
        public bool Start() 
        {
            try
            {
                var host = new WebHostBuilder()
                    .UseKestrel()
                    .ConfigureLogging((hostingContext, logging) =>
                    {
                        logging.ClearProviders(); 
                    })
                    .Configure(a => a.Run(ProcessAsync))
                    .Build();
                
                host.RunAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
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
                return null;
            }
        }
        
        /// <summary>
        /// Verifies the request
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

            int reqId = -1;

            try
            {
                // read id
                reqId = request["id"].ToObject<int>();
                
                string methodName = JToken.FromObject(request["method"]).ToObject<string>();
                JObject reqParams = JObject.FromObject(request["params"]);

                JObject responseData = null;
                switch (methodName)
                {
                       case GetTxMethodName:
                           responseData = await ProcessGetTx(reqParams);
                           break;
                       case InsertTxMethodName:
                           responseData = await InsertTx(reqParams);
                           break;
                       default:
                           Console.WriteLine("Method name not found"); // todo log
                           break;
                }

                if (responseData == null)
                {
                    // todo write error
                }

                JObject resp = CreateResponse(responseData, reqId);
                
                await WriteResponse(context, resp);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private JObject CreateResponse(JObject responseData, int id)
        {
            JObject jObj = new JObject
            {
                ["jsonrpc"] = "2.0",
                ["id"] = id,
                ["result"] = responseData
            };

            return jObj;
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
        
        private async Task<JObject> InsertTx(JObject reqParams)
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