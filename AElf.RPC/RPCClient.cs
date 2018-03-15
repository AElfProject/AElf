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
        
    }
}