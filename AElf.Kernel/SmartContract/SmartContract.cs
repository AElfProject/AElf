using System.Threading.Tasks;
using Google.Protobuf;

namespace AElf.Kernel
{
    public abstract class SmartContract : ISmartContract
    {
        private IAccountDataProvider _accountDataProvider;
        private ISerializer<SmartContractRegistration> _serializer;

        protected SmartContract(ISerializer<SmartContractRegistration> serializer)
        {
            _serializer = serializer;
        }

        public async Task InitializeAsync(IAccountDataProvider dataProvider)
        {
            _accountDataProvider = dataProvider;
            await Task.CompletedTask;
        }

        public abstract Task InvokeAsync(SmartContractInvokeContext context);

    }
}