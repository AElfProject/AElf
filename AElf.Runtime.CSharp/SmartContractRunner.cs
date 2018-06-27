using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.KernelAccount;
using Google.Protobuf;
using Path = System.IO.Path;
using AElf.ABI.CSharp;
using Mono.Cecil;
using Module = AElf.ABI.CSharp.Module;

namespace AElf.Runtime.CSharp
{
    public class SmartContractRunner : ISmartContractRunner
    {
        private readonly string _sdkDir;
        private readonly AssemblyChecker _assemblyChecker;

        public SmartContractRunner(IConfig config) : this(config.SdkDir, config.BlackList, config.WhiteList)
        {
        }

        public SmartContractRunner(string sdkDir, IEnumerable<string> blackList=null, IEnumerable<string> whiteList=null)
        {
            _sdkDir = Path.GetFullPath(sdkDir);
            _assemblyChecker = new AssemblyChecker(blackList, whiteList);
        }

        /// <summary>
        /// Creates an isolated context for the smart contract residing with an Api singleton.
        /// </summary>
        /// <returns></returns>
        private ContractCodeLoadContext GetLoadContext()
        {
            // To make sure each smart contract resides in an isolated context with an Api singleton
            return new ContractCodeLoadContext(_sdkDir);
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

            var abiModule = GetAbiModule(reg);
            // TODO: Change back
            var type = assembly.GetTypes().FirstOrDefault(x => x.FullName == abiModule.Name);
            if (type == null)
            {
                throw new InvalidCodeException($"No SmartContract type {abiModule.Name} is defined in the code.");
            }

            var instance = (ISmartContract) Activator.CreateInstance(type);

            var ApiSingleton = loadContext.Sdk.GetTypes().FirstOrDefault(x => x.Name.EndsWith("Api"));

            if (ApiSingleton == null)
            {
                throw new InvalidCodeException("No Api was found.");
            }

            Executive executive = new Executive(abiModule).SetSmartContract(instance).SetApi(ApiSingleton);

            return await Task.FromResult(executive);
        }

        private Module GetAbiModule(SmartContractRegistration reg)
        {
            var code = reg.ContractBytes.ToByteArray();
            var abiModule = Generator.GetABIModule(code);
            return abiModule;
        }

        public IMessage GetAbi(SmartContractRegistration reg)
        {
            return GetAbiModule(reg);
        }

        public System.Type GetContractType(SmartContractRegistration reg)
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

            var abiModule = Generator.GetABIModule(code);
            // TODO: Change back
            var type = assembly.GetTypes().FirstOrDefault(x => x.FullName == abiModule.Name);
            if (type == null)
            {
                throw new InvalidCodeException($"No SmartContract type {abiModule.Name} is defined in the code.");
            }

            return type;
        }

        /// <summary>
        /// Performs code checks.
        /// </summary>
        /// <param name="code">The code to be checked.</param>
        /// <param name="isPrivileged">Is the contract deployed by system user.</param>
        /// <exception cref="InvalidCodeException">Thrown when issues are found in the code.</exception>
        public void CodeCheck(byte[] code, bool isPrivileged)
        {
            var modDef = ModuleDefinition.ReadModule(new MemoryStream(code));
            var forbiddenTypeRefs = _assemblyChecker.GetBlackListedTypeReferences(modDef);
            if (isPrivileged)
            {
                // Allow system user to use multi-thread
                forbiddenTypeRefs = forbiddenTypeRefs.Where(x => !x.FullName.StartsWith("System.Threading")).ToList();
            }
            if (forbiddenTypeRefs.Count > 0)
            {
                throw new InvalidCodeException($"\nForbidden type references detected:\n{string.Join("\n  ", forbiddenTypeRefs.Select(x=>x.FullName))}");
            }
        }
    }
}