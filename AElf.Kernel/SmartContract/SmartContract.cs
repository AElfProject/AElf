using System;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Google.Protobuf;

namespace AElf.Kernel
{
    public abstract class SmartContract : ISmartContract
    {
        private IAccountDataProvider _accountDataProvider;


        public async Task InitializeAsync(IAccountDataProvider dataProvider)
        {
            _accountDataProvider = dataProvider;
            await Task.CompletedTask;
        }

        public abstract Task<object> InvokeAsync(SmartContractInvokeContext context);

    }
    
    public class CSharpSmartContract : SmartContract
    {

        public object Instance { get; set; }
        public override async Task<object> InvokeAsync(SmartContractInvokeContext context)
        {
            var type = Instance.GetType();
            
            // method info
            var member = type.GetMethod(context.MethodName);

            // params array
            var parameters = Parameters.Parser.ParseFrom(context.Params).Params.Select(p => p.Value()).ToArray();
            
            return (object)member.Invoke(Instance, parameters);
        }
    }
}