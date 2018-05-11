using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
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
        
        private JObject ValidateRequest()
        {
            // TODO validation
            return null;
        }
        
        private async Task ProcessAsync(HttpContext context)
        {
            if (context?.Request?.Body == null)
                return;
            
            JObject request = ParseRequest(context);
            
            if (request == null)
            {
                JObject err = ErrorResponseFactory.GetParseErrorObj(0);
                WriteResponse(context, err);
            }
            
            JObject validErr = ValidateRequest();
            
            if (validErr != null)
                WriteResponse(context, validErr);
            
            if (context.Request.Method == "POST")
            {
                try
                {
                    JToken method = JToken.FromObject(request["method"]);
                    JArray paramArray = JArray.FromObject(request["params"]);

                    if (method.ToObject<string>() == "get_tx")
                    {
                        JToken i1 = paramArray[0];
                        string txid = i1.ToObject<string>();

                        byte[] arr = StringToByteArray(txid);

                        ITransaction tx = await _node.GetTransaction(arr);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                /*if (method == "get_tx")
                {
                    byte[] txid = 
                }*/
            }
            
            // write to stream
            /*string bodyAsString;
            using (var streamReader = new StreamReader(context.Request.Body, Encoding.UTF8))
            {
                bodyAsString = streamReader.ReadToEnd();
            }
            Console.WriteLine("Request data: " + bodyAsString);
            await context.Response.WriteAsync("hello");*/
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