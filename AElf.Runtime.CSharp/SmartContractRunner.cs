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

    public class SmartContractRunner : ISmartContractRunner
    {
        private readonly string _apiDllDirectory;

        public SmartContractRunner(string apiDllDirectory)
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

        public async Task<IExecutive> RunAsync(SmartContractRegistration reg)
        {
            // TODO: Maybe input arguments can be simplified

            var code = reg.ContractBytes.ToByteArray();

            var loadContext = GetLoadContext();

            Assembly assembly = null;
            using (Stream stream = new MemoryStream(code))
            {
                assembly = loadContext.LoadFromStream(stream);
            }

            if (assembly == null)
            {
                throw new InvalidCodeException("Invalid binary code.");
            }

            var type = assembly.GetTypes().FirstOrDefault(x => x.BaseType.Name.EndsWith("CSharpSmartContract"));
            if (type == null)
            {
                throw new InvalidCodeException("No SmartContract type is defined in the code.");
            }

            var instance = (ISmartContract) Activator.CreateInstance(type);

            var ApiSingleton = loadContext.Sdk.GetTypes().FirstOrDefault(x => x.Name.EndsWith("Api"));

            if (ApiSingleton == null)
            {
                throw new InvalidCodeException("No Api was found.");
            }

            Executive executive = new Executive().SetSmartContract(instance).SetApi(ApiSingleton);

            return await Task.FromResult(executive);
        }
    }
}