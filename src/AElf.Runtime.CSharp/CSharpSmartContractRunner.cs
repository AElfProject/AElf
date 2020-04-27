using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Types;

namespace AElf.Runtime.CSharp
{
    public class CSharpSmartContractRunner : ISmartContractRunner
    {
        public int Category { get; protected set; }
        private readonly ISdkStreamManager _sdkStreamManager;

        public CSharpSmartContractRunner(
            string sdkDir)
        {
            var sdkDir1 = Path.GetFullPath(sdkDir);
            _sdkStreamManager = new SdkStreamManager(sdkDir1);
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

            Assembly assembly = LoadAssembly(code, loadContext);

            if (assembly == null)
            {
                throw new InvalidAssemblyException("Invalid binary code.");
            }

            ContractVersion = assembly.GetName().Version?.ToString();

            var executive = new Executive(assembly, loadContext)
            {
                ContractHash = reg.CodeHash,
                IsSystemContract = reg.IsSystemContract,
                ContractVersion = ContractVersion
            };


            return await Task.FromResult(executive);
        }

        protected virtual Assembly LoadAssembly(byte[] code, AssemblyLoadContext loadContext)
        {
            Assembly assembly;
            using (Stream stream = new MemoryStream(code))
            {
                assembly = loadContext.LoadFromStream(stream);
            }

            return assembly;
        }

        public string ContractVersion { get; protected set; }
    }
}