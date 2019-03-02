using System;
using System.Collections.Generic;
using System.IO;
using AElf.Common;
using AElf.Contracts.Consensus.DPoS;
using AElf.Contracts.Genesis;
using AElf.Contracts.Token;
using AElf.Kernel;
using AElf.Kernel.KernelAccount;
using AElf.Types.CSharp;
using Google.Protobuf;

namespace AElf.OS.Rpc
{
    public class RpcTestHelper
    {
        public static Transaction[] GetGenesisTransactions(int chainId, Address account)
        {
            var transactions = new List<Transaction>
            {
                GetTransactionForDeployment(chainId, typeof(BasicContractZero)),
                GetTransactionForDeployment(chainId, typeof(ConsensusContract)),
                GetTransactionForDeployment(chainId, typeof(TokenContract)),
                GetTransactionForTokenInitialize(chainId, account)
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

        public static Transaction GetTransactionForTokenInitialize(int chainId, Address account)
        {
            var zeroAddress = Address.BuildContractAddress(chainId, 1);

            return new Transaction()
            {
                From = account,
                To = zeroAddress,
                MethodName = "Initialize",
                Params = ByteString.CopyFrom(ParamsPacker.Pack("ELF", "ELF_Token", 100000, 5))
            };
        }
    }
}