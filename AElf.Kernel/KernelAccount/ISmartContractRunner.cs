using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AElf.Kernel.SmartContracts.CSharpSmartContract;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.KernelAccount
{
    public interface ISmartContractRunner
    {
        Task<ISmartContract> RunAsync(SmartContractRegistration reg, SmartContractDeployment deployment, 
            IAccountDataProvider adp);
    }
    
    /*public class KernelZeroSmartContractRunner: ISmartContractRunner
    {
        public Task<ISmartContract> RunAsync(SmartContractRegistration reg, SmartContractDeployment deployment)
        {
            throw new NotImplementedException();
        }
    }*/

    public class CSharpSmartContractRunner : ISmartContractRunner
    {

        public async Task<ISmartContract> RunAsync(SmartContractRegistration reg, SmartContractDeployment deployment, 
            IAccountDataProvider adp)
        {
            
            var contractName = StringValue.Parser.ParseFrom(reg.ContractBytes).Value;
            var type = System.Type.GetType(contractName);
            
            // construct instance
            var constructorParams = Parameters.Parser.ParseFrom(deployment.ConstructParams).Params;
            var parameterObjs = constructorParams.Select(p => p.Value()).ToArray();
            var paramTypes = parameterObjs.Select(p => p.GetType()).ToArray();
            var constructorInfo = type.GetConstructor(paramTypes);

            var instance = constructorInfo.Invoke(parameterObjs);
            
            // inject instance
            var smartContract = new CSharpSmartContract
            {
                Instance = instance
            };
            
            // initialize account info 
            await smartContract.InitializeAsync(adp);
            return smartContract;
        }
    } 
}