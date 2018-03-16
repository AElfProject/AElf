using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Grpc.Core;

namespace AElf.RPC
{
    public class SmartContractExecution : AElfRPC.AElfRPCBase
    {
        public override Task<Result> Invoke(InvokeOption request, ServerCallContext context)
        {
            var res = new Result();
            try
            {
                var smartContracRegistration = request.Reg;
                var bytecode = smartContracRegistration.Byte;
                var assembly = Assembly.Load(bytecode.ToByteArray());
                var type = assembly.GetType(request.ClassName);
                var instance = assembly.CreateInstance(request.ClassName);

                var method = type.GetMethod(request.MethodName);
                var paramList = request.Params;
            
                var objs = TypeConverter.Convert(paramList);
            
                method.Invoke(instance, objs);
                
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                res.Res = "Fail!";
                throw;
            }
            
            res.Res = "Success!";
            return Task.FromResult(res);

        }
        
        
        public override async Task ListResults(InvokeOption request, IServerStreamWriter<Result> responseStream, ServerCallContext context)
        {
            try
            {
                var smartContracRegistration = request.Reg;
                var bytecode = smartContracRegistration.Byte;
                var assembly = Assembly.Load(bytecode.ToByteArray());
                var type = assembly.GetType(request.ClassName);
                var instance = assembly.CreateInstance(request.ClassName);
                
                var objs = TypeConverter.Convert(request.Params);
                var method = type.GetMethod(request.MethodName);

                for (var i = 1; i <= 3; i++)
                {
                    await (Task) method.Invoke(instance, objs);
                    var response = new Result
                    {
                        Res = i.ToString()
                    };
                    await responseStream.WriteAsync(response);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }


        public override async Task<Result> ListInvoke(IAsyncStreamReader<InvokeOption> requestStream, ServerCallContext context)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            while (await requestStream.MoveNext())
            {
                var option = requestStream.Current;
                var smartContracRegistration = option.Reg;
                var bytecode = smartContracRegistration.Byte;
                var assembly = Assembly.Load(bytecode.ToByteArray());
                var type = assembly.GetType(option.ClassName);
                var instance = assembly.CreateInstance(option.ClassName);
                
                var objs = TypeConverter.Convert(option.Params);
                var method = type.GetMethod(option.MethodName);

                await (Task) method.Invoke(instance, objs);
            }
            stopWatch.Stop();

            var result = new Result
            {
                Res = stopWatch.ElapsedMilliseconds.ToString()
            };

            return result;
        }

    }
}