using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Types;

namespace AElf.Runtime.CSharp
{
    public class SmartContractRunnerForCategoryZero : ISmartContractRunner
    {
        public int Category { get; protected set; }
        private readonly ISdkStreamManager _sdkStreamManager;

        private readonly ConcurrentDictionary<string, MemoryStream> _cachedSdkStreams =
            new ConcurrentDictionary<string, MemoryStream>();

        private readonly ConcurrentDictionary<Hash, Type> _cachedContractTypeByHash =
            new ConcurrentDictionary<Hash, Type>();

        private readonly string _sdkDir;
        private readonly ContractAuditor _contractAuditor;

        protected readonly IServiceContainer<IExecutivePlugin> _executivePlugins;

        public SmartContractRunnerForCategoryZero(
            string sdkDir,
            IServiceContainer<IExecutivePlugin> executivePlugins = null,
            IEnumerable<string> blackList = null,
            IEnumerable<string> whiteList = null)
        {
            _sdkDir = Path.GetFullPath(sdkDir);
            _sdkStreamManager = new SdkStreamManager(_sdkDir);
            _contractAuditor = new ContractAuditor(blackList, whiteList);
            _executivePlugins = executivePlugins ?? ServiceContainerFactory<IExecutivePlugin>.Empty;
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

            var executive = new Executive(assembly, _executivePlugins) {ContractHash = reg.CodeHash};

            return await Task.FromResult(executive);
        }

        public byte[] CodePatch(byte[] code)
        {
            // Disabled due to timeout during unit tests
#if !UNIT_TEST
            return ContractPatcher.Patch(code);
#else
            return code;
#endif
        }

        /// <summary>
        /// Performs code checks.
        /// </summary>
        /// <param name="code">The code to be checked.</param>
        /// <param name="isPrivileged">Is the contract deployed by system user.</param>
        /// <exception cref="InvalidCodeException">Thrown when issues are found in the code.</exception>
        public void CodeCheck(byte[] code, bool isPrivileged)
        {
#if !UNIT_TEST
            _contractAuditor.Audit(code, isPrivileged);
#endif
        }
    }
}