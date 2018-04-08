using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;
using Google.Protobuf;

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

        public async Task InitializeAsync(IAccountDataProvider dataProvider)
        {
            _accountDataProvider = dataProvider;
            await Task.CompletedTask;
        }

        protected void Resolve(SmartContractRegistration smartContractRegistration)
        {
            Data = _serializer.Serialize(smartContractRegistration);
        }

        public abstract Task InvokeAsync(IHash caller, string methodname, ByteString bytes);

    }

    public class CSharpSmartContract : SmartContract
    {
        public CSharpSmartContract(ISerializer<SmartContractRegistration> serializer, SmartContractRegistration smartContractRegistration) : base(serializer)
        {
            Resolve(smartContractRegistration);
        }
        
        public override Task InvokeAsync(IHash caller, string methodname, ByteString bytes)
        {
            throw new NotImplementedException();
        }
    }
}