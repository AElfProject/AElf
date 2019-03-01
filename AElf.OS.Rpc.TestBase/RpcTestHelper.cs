using System;
using System.Collections.Generic;
using System.IO;
using AElf.Common;
using AElf.Contracts.Genesis;
using AElf.Kernel;
using AElf.Kernel.KernelAccount;
using AElf.Types.CSharp;
using Google.Protobuf;

namespace AElf.OS.Rpc
{
    public class RpcTestHelper
    {
        public static Transaction[] GetGenesisTransactions(int chainId)
        {
            var transactions = new List<Transaction>
            {
                GetTransactionForDeployment(chainId, typeof(BasicContractZero))
            };

            return transactions.ToArray();
        }

        public static Transaction GetTransactionForDeployment(int chainId, Type contractType)
        {
            var zeroAddress = Address.BuildContractAddress(chainId, 0);
            var code = File.ReadAllBytes(contractType.Assembly.Location);
            return new Transaction()
            {
                From = zeroAddress,
                To = zeroAddress,
                MethodName = nameof(ISmartContractZero.DeploySmartContract),
                Params = ByteString.CopyFrom(ParamsPacker.Pack(2, code))
            };
        }
    }
}