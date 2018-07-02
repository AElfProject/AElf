using System;
using System.IO;
using Google.Protobuf;
using ServiceStack;

namespace AElf.Kernel.Tests
{
    public static class ContractCodes
    {
        public static readonly string TestContractName = "AElf.Kernel.Tests.TestContract";

        public static readonly string TestContractZeroName = "AElf.Contracts.Genesis";

        public static string TestContractFolder
        {
            get
            {
                return $"../../../../{TestContractName}/bin/Debug/netstandard2.0";
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
                byte[] code = null;
                using (FileStream file = File.OpenRead(System.IO.Path.GetFullPath(TestContractDllPath)))
                {
                    code = file.ReadFully();
                }
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
                byte[] code = null;
                using (FileStream file = File.OpenRead(System.IO.Path.GetFullPath(TestContractZeroDllPath)))
                {
                    code = file.ReadFully();
                }
                return code;
            }
        }
    }
}
