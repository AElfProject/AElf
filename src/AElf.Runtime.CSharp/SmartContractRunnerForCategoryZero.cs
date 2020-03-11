using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using AElf.CSharp.CodeOps;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Types;

namespace AElf.Runtime.CSharp
{
    public class SmartContractRunnerForCategoryZero : ISmartContractRunner
    {
        public int Category { get; protected set; }
        private readonly ISdkStreamManager _sdkStreamManager;

        //TODO: remove
        private readonly ConcurrentDictionary<string, MemoryStream> _cachedSdkStreams =
            new ConcurrentDictionary<string, MemoryStream>();

        private readonly ConcurrentDictionary<Hash, Type> _cachedContractTypeByHash =
            new ConcurrentDictionary<Hash, Type>();

        private readonly string _sdkDir;


        public SmartContractRunnerForCategoryZero(
            string sdkDir)
        {
            _sdkDir = Path.GetFullPath(sdkDir);
            _sdkStreamManager = new SdkStreamManager(_sdkDir);
        }

        /// <summary>
        /// Creates an isolated context for the smart contract residing with an Api singleton.
        /// </summary>
        /// <returns></returns>
        protected virtual AssemblyLoadContext GetLoadContext()
        {
            // To make sure each smart contract resides in an isolated context with an Api singleton
            return new ContractCodeLoadContext(_sdkStreamManager);
        }

        public virtual async Task<IExecutive> RunAsync(SmartContractRegistration reg)
        {
            var code = reg.Code.ToByteArray();

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

            var executive = new Executive(assembly)
            {
                ContractHash = reg.CodeHash,
                IsSystemContract = reg.IsSystemContract
            };

            return await Task.FromResult(executive);
        }
    }
}