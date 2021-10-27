using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using AElf.Kernel;

namespace AElf.Runtime.CSharp
{
    public class UnitTestCSharpSmartContractRunner : CSharpSmartContractRunner
    {
        public UnitTestCSharpSmartContractRunner(string sdkDir)
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
            }

            return assembly;
        }

    }
}