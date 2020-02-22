using System;
using System.IO;
using System.Reflection;
using AElf.TestBase;

namespace AElf.CSharp.CodeOps
{
    public class CSharpCodeOpsTestBase : AElfIntegratedTest<TestCSharpCodeOpsAElfModule>
    {
        protected const string ContractPatchedDllDir = "../../../../patched/";
        
        protected byte[] ReadContractCode(Type contractType)
        {
            var location = Assembly.GetAssembly(contractType).Location;
            return ReadCode( location);
        }

        protected byte[] ReadPatchedContractCode(Type contractType)
        {
            return ReadCode(ContractPatchedDllDir + contractType.Module + ".patched");
        }

        private byte[] ReadCode(string path)
        {
            return File.Exists(path)
                ? File.ReadAllBytes(path)
                : throw new FileNotFoundException("Contract DLL cannot be found. " + path);
        }
    }
}