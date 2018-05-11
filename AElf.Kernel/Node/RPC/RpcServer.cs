using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using AElf.Node.RPC.DTO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace AElf.Kernel.Node.RPC
{
    public class RpcServer : IRpcServer
    {

        private const string GetTxMethodName = "get_tx";
        private const string InsertTxMethodName = "insert_tx";
        
        private List<string> _rpcCommands = new List<string>()
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
        
        //curl -H "Content-Type: application/json" -X POST -d '{"jsonrpc": "2.0", "method": "get_tx", "params": ["68656c6c6f776f726c64", 23], "id": 1}'  http://localhost:5000/ -m 


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
        /// Verifies a certain amount of points about the request
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
                WriteResponse(context, err);
                return;
            }
            
            JObject validErr = ValidateRequest(request);

            if (validErr != null)
            {
                WriteResponse(context, validErr);
                return;
            }

            try
            {
                string methodName = JToken.FromObject(request["method"]).ToObject<string>();
                JObject reqParams = JObject.FromObject(request["params"]);

                JObject response = null;
                switch (methodName)
                {
                       case GetTxMethodName:
                           response = await ProcessGetTx(reqParams);
                           break;
                       case InsertTxMethodName:
                           response = await InsertTxName(reqParams);
                           break;
                       default:
                           Console.WriteLine("Method name not found"); // todo log
                           break;
                }

                if (response == null)
                {
                    
                }
                
                WriteResponse(context, response);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private async Task<JObject> ProcessGetTx(JObject reqParams)
        {
            ITransaction tx = await _node.GetTransaction(new byte[] { 0x01, 0x02});
            TransactionDto txDto = tx.ToTransactionDto();
            
            return JObject.FromObject(txDto);
        }
        
        private async Task<JObject> InsertTxName(JObject reqParams)
        {
            JObject jObj = new JObject();

            return jObj;
        }

        public void WriteResponse(HttpContext context, JObject response)
        {
            context?.Response?.WriteAsync(response.ToString(), Encoding.UTF8);
        }

        public byte[] StringToByteArray(String hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }
        
        
    }
}