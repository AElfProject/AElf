using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.SmartContract.Infrastructure;

namespace AElf.Runtime.CSharp
{
    public class SmartContractRunnerForCategoryThirty : SmartContractRunnerForCategoryZero
    {
        public SmartContractRunnerForCategoryThirty(string sdkDir, IServiceContainer<IExecutivePlugin> executivePlugins,
            IEnumerable<string> blackList = null, IEnumerable<string> whiteList = null) : base(sdkDir, executivePlugins,
            blackList, whiteList)
        {
            Category = KernelConstants.CodeCoverageRunnerCategory;
        }

        /*protected override AssemblyLoadContext GetLoadContext()
        {
            return AssemblyLoadContext.Default;
        }*/

        public override async Task<IExecutive> RunAsync(SmartContractRegistration reg)
        {
            var code = reg.Code.ToByteArray();

            var loadContext = GetLoadContext();

            Assembly assembly = null;
            using (Stream stream = new MemoryStream(code))
            {
                assembly = loadContext.LoadFromStream(stream);
                
                //load by main context, not load in code, directly load in dll

                try
                {
                    var assembly2 = Assembly.Load(assembly.FullName);

                    if (code.SequenceEqual(File.ReadAllBytes(assembly2.Location)))
                    {
                        assembly = assembly2;
                    }
                }
                catch(Exception)
                {
                    //may cannot find assembly in local
                }
            }

            if (assembly == null)
            {
                throw new InvalidCodeException("Invalid binary code.");
            }

            var executive = new Executive(assembly, _executivePlugins);

            return await Task.FromResult(executive);
        }
    }
}