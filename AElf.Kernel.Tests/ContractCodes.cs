using System;
using System.IO;
using Google.Protobuf;
namespace AElf.Kernel.Tests
{
    public static class ContractCodes
    {
        public static readonly string TestContractName = "AElf.Kernel.Tests.TestContract";

        public static readonly string TestContractZeroName = "AElf.Contracts.Genesis";

        private static readonly string TestSDKPath = "AElf.Sdk.CSharp";

        public static string TestContractFolder
        {
            get
            {
                return $"../../../../{TestContractName}/bin/Debug/netstandard2.0";
            }
        }
        
        public static string TestSDKPathFolder
        {
            get
            {
                return $"../../../../{TestSDKPath}/bin/Debug/netstandard2.0";
            }
        }

        public static string TestContractDllPath
        {
            get
            {
                return $"{TestContractFolder}/{TestContractName}.dll";
            }
        }


        public static byte[] TestContractCode
        {
            get
            {
                byte[] code = File.ReadAllBytes(System.IO.Path.GetFullPath(TestContractDllPath));

                return code;
            }
        }

        public static string TestContractZeroFolder
        {
            get
            {
                return $"../../../../{TestContractZeroName}/bin/Debug/netstandard2.0";
            }
        }

        public static string TestContractZeroDllPath
        {
            get
            {
                return $"{TestContractZeroFolder}/{TestContractZeroName}.dll";
            }
        }

        public static byte[] TestContractZeroCode
        {
            get
            {
                byte[] code = File.ReadAllBytes(System.IO.Path.GetFullPath(TestContractZeroDllPath));
                return code;
            }
        }
    }
}
