using System;
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

    }
}