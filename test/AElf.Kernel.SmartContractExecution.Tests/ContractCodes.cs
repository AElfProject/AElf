using System;
using System.IO;

namespace AElf.Kernel.Tests
{
    public static class ContractCodes
    {
        public static readonly string TestContractName = "AElf.Kernel.Core.Tests.TestContract";

        public static readonly string TestContractZeroName = "AElf.Contracts.Genesis";

        private static readonly string TestSDKPath = "AElf.Sdk.CSharp";

        public static string TestContractFolder => $"../../../../{TestContractName}/bin/Debug/netstandard2.0";

        public static string TestSDKPathFolder => $"../../../../{TestSDKPath}/bin/Debug/netstandard2.0";

        public static string TestContractDllPath => $"{TestContractFolder}/{TestContractName}.dll";


        public static byte[] TestContractCode => File.ReadAllBytes(Path.GetFullPath(TestContractDllPath));

        public static string TestContractZeroFolder => $"../../../../{TestContractZeroName}/bin/Debug/netstandard2.0";

        public static string TestContractZeroDllPath => $"{TestContractZeroFolder}/{TestContractZeroName}.dll";

        public static byte[] TestContractZeroCode => File.ReadAllBytes(Path.GetFullPath(TestContractZeroDllPath));
    }
}
