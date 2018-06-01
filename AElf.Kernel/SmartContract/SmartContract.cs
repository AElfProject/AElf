﻿using System.Linq;
using System.Threading.Tasks;

namespace AElf.Kernel
{
    public abstract class SmartContract : ISmartContract
    {
        private readonly IAccountDataProvider _accountDataProvider;


        public SmartContract(IAccountDataProvider dataProvider)
        {
            _accountDataProvider = dataProvider;
        }

        public abstract Task<object> InvokeAsync(SmartContractInvokeContext context);

    }



    
    public class CSharpSmartContract : SmartContract
    {
        public CSharpSmartContract(IAccountDataProvider dataProvider) : base (dataProvider)
        {

        }

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