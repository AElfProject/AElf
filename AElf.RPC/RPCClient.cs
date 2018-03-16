using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.RPC
{
    public class SmartContract
    {
        private readonly AElfRPC.AElfRPCClient _client;
        private readonly SmartContractReg _registration;

        public SmartContract(AElfRPC.AElfRPCClient client, SmartContractReg registration)
        {
            _client = client;
            _registration = registration;
        }

        /// <summary>
        /// end-to-end simple rpc
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="objs"></param>
        /// <returns></returns>
        public Task<Result> Invoke(string methodName, params object[] objs)
        {
            
            var paramList = new ParamList();
            paramList.SetParam(objs);

            // create request options
            var options = new InvokeOption
            {
                ClassName = _registration.Name,
                MethodName = methodName,
                Reg = _registration,
                Params = paramList
            };

            return Task.FromResult(_client.Invoke(options));
        }


        /// <summary>
        /// server side stream rpc
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="objs"></param>
        /// <returns></returns>
        public async Task ListResults(string methodName, params object[] objs)
        {
            var paramList = new ParamList();
            paramList.SetParam(objs);

            // create request options
            var options = new InvokeOption
            {
                ClassName = _registration.Name,
                Reg = _registration,
                Params = paramList,
                MethodName = methodName
            };

            try
            {
                using (var call = _client.ListResults(options))
                {
                    var responseStream = call.ResponseStream;

                    while (await responseStream.MoveNext())
                    {
                        var result = responseStream.Current;
                        Console.WriteLine(result.Res);
                    }
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("RPC Failed");
                throw;
            }
            
        }


        /// <summary>
        /// client side stream rpc
        /// </summary>
        /// <returns></returns>
        public async Task ListInvoke(string methodName, params object[] objs)
        {
            
            using (var  call = _client.ListInvoke())
            {
                var paramList = new ParamList();
                paramList.SetParam(objs);
                
                var options = new InvokeOption
                {
                    ClassName = _registration.Name,
                    Reg = _registration,
                    Params = paramList,
                    MethodName = methodName
                };
                
                for (var i = 1; i <= 3; i++)
                {
                    await call.RequestStream.WriteAsync(options);
                    await Task.Delay(500);    
                }

                await call.RequestStream.CompleteAsync();
                
                var summary = await call.ResponseAsync;
            }   
        }



        public async Task BiDirectional(string methodName, params object[] objs)
        {
            using (var  call = _client.BiDirectional())
            {
                var responseReaderTask = Task.Run(async () =>
                {
                    while (call != null && await call.ResponseStream.MoveNext())
                    {
                        var res = call.ResponseStream.Current;
                        Console.WriteLine("Elapsed time : {0} ms", res.Res);
                    }
                });
                
                
                var paramList = new ParamList();
                paramList.SetParam(objs);
                
                var options = new InvokeOption
                {
                    ClassName = _registration.Name,
                    Reg = _registration,
                    Params = paramList,
                    MethodName = methodName
                };
                
                for (var i = 1; i <= 3; i++)
                {
                    await call.RequestStream.WriteAsync(options);
                    await Task.Delay(500);    
                }

                await call.RequestStream.CompleteAsync();
                await responseReaderTask;
                
            }
        }
    }
}