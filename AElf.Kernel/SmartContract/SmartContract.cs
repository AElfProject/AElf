using System;
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

        public abstract Task InvokeAsync(SmartContractInvokeContext context);

    }
    
    public class CSharpSmartContract : SmartContract
    {
        public Type Type { get; set; }
        public ConstructorInfo Constructor { get; set; }
        public object[] Params { get; set; }
        
        public override Task InvokeAsync(SmartContractInvokeContext context)
        {
            throw new System.NotImplementedException();
        }
    }
}