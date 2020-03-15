using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using AElf.CSharp.CodeOps;
using AElf.Kernel;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Types;

namespace AElf.Runtime.CSharp
{
    public class SmartContractRunnerForCategoryThirty : SmartContractRunnerForCategoryZero
    {
        public SmartContractRunnerForCategoryThirty(string sdkDir)
            : base(sdkDir)
        {
            Category = KernelConstants.CodeCoverageRunnerCategory;
        }

        protected override Assembly LoadAssembly(byte[] code, AssemblyLoadContext loadContext)
        {
            var assembly = base.LoadAssembly(code, loadContext);

            Assembly assembly2 = null;
            try
            {
                assembly2 = Assembly.Load(assembly.FullName);
            }
            catch (Exception)
            {
                //may cannot find assembly in local
            }

            if (assembly2 != null && code.SequenceEqual(File.ReadAllBytes(assembly2.Location)))
            {
                assembly = assembly2;
                loadContext.Unload();
            }

            return assembly;
        }

    }
}