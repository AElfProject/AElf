using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.KernelAccount;
using Google.Protobuf;
using Path = System.IO.Path;

namespace AElf.Runtime.CSharp
{
    public class InvalidCodeException : Exception
    {
        public InvalidCodeException(string message) : base(message)
        {
        }
    }
    
    public class BinaryCSharpSmartContractRunner : ISmartContractRunner
    {
        private readonly string _apiDllDirectory;

        public BinaryCSharpSmartContractRunner(string apiDllDirectory)
        {
            _apiDllDirectory = Path.GetFullPath(apiDllDirectory);
        }

        /// <summary>
        /// Creates an isolated context for the smart contract residing with an Api singleton.
        /// </summary>
        /// <returns></returns>
        private CSharpAssemblyLoadContext GetLoadContext()
        {
            // To make sure each smart contract resides in an isolated context with an Api singleton
            return new CSharpAssemblyLoadContext(_apiDllDirectory, AppDomain.CurrentDomain.GetAssemblies());
        }
        
        public async Task<ISmartContract> RunAsync(SmartContractRegistration reg, SmartContractDeployment deployment, 
            IAccountDataProvider adp)
        {
            // TODO: Maybe input arguments can be simplified

            var code = reg.ContractBytes.ToByteArray();
            
            Assembly assembly = null;
            using (Stream stream = new MemoryStream(code))
            {
                assembly = GetLoadContext().LoadFromStream(stream);
            }

            if (assembly == null)
            {
                throw new InvalidCodeException("Invalid binary code.");
            }
            
            var type = assembly.GetTypes().FirstOrDefault(x => x.BaseType.Name.EndsWith("CSharpSmartContract"));
            if(type == null)
            {
                throw new InvalidCodeException("No SmartContract type is defined in the code.");
            }
            
            // construct instance
            var constructorParams = Parameters.Parser.ParseFrom(deployment.ConstructParams).Params;
            var parameterObjs = constructorParams.Select(p => p.Value()).ToArray();
            var paramTypes = parameterObjs.Select(p => p.GetType()).ToArray();
            var constructorInfo = type.GetConstructor(paramTypes);

            // TODO: There will be no parameters passed to constructor as the context will be injected
            var instance = (ISmartContractWithContext) constructorInfo.Invoke(parameterObjs);
            instance?.SetDataProvider(adp.GetDataProvider());

            return await Task.FromResult(instance);
        }
    }
}