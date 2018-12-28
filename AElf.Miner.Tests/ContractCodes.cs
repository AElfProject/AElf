using System.IO;

namespace AElf.Miner.Tests
{
    public static class ContractCodes
    {
        public static readonly string TestContractName = "AElf.Kernel.Tests.TestContract";

        public static readonly string TestContractZeroName = "AElf.Contracts.Genesis";

        public static string TestContractFolder => $"../../../../{TestContractName}/bin/Debug/netstandard2.0";

        public static string TestContractDllPath => $"{TestContractFolder}/{TestContractName}.dll";


        public static byte[] TestContractCode => File.ReadAllBytes(Path.GetFullPath(TestContractDllPath));

        public static string TestContractZeroFolder => $"../../../../{TestContractZeroName}/bin/Debug/netstandard2.0";

        public static string TestContractZeroDllPath => $"{TestContractZeroFolder}/{TestContractZeroName}.dll";

        public static byte[] TestContractZeroCode => File.ReadAllBytes(Path.GetFullPath(TestContractZeroDllPath));
    }
}