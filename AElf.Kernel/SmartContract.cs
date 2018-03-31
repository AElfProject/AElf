using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;

namespace AElf.Kernel
{
    public abstract class SmartContract : ISmartContract
    {
        private IAccountDataProvider _accountDataProvider;
        private ISerializer<SmartContractRegistration> _serializer;
        protected byte[] Data { get; set; }

        protected SmartContract(ISerializer<SmartContractRegistration> serializer)
        {
            _serializer = serializer;
        }

        public async Task InititalizeAsync(IAccountDataProvider dataProvider)
        {
            _accountDataProvider = dataProvider;
            await Task.CompletedTask;
        }

        protected void Resolve(SmartContractRegistration smartContractRegistration)
        {
            Data = _serializer.Serialize(smartContractRegistration);
        }

        public abstract Task InvokeAsync(IHash caller, string methodname, params object[] objs);
        public abstract IHash GetHash();
    }

    public class CSharpSmartContract : SmartContract
    {
        public CSharpSmartContract(ISerializer<SmartContractRegistration> serializer, SmartContractRegistration smartContractRegistration) : base(serializer)
        {
            Resolve(smartContractRegistration);
        }
        
        public override Task InvokeAsync(IHash caller, string methodname, params object[] objs)
        {
            throw new NotImplementedException();
        }

        public override IHash GetHash()        
        {
            throw new NotImplementedException();
        }
    }
}