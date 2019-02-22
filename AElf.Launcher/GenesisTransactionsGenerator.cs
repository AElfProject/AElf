using System;
using System.Collections.Generic;
using System.IO;
using AElf.Common;
using AElf.Contracts.Genesis;
using AElf.Kernel;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Node.Application;
using AElf.Modularity;
using AElf.Types.CSharp;
using Google.Protobuf;

namespace AElf.Launcher
{
    public class GenesisTransactionsGenerator
    {
        private string _assemblyPath = Path.GetDirectoryName(typeof(GenesisTransactionsGenerator).Assembly.Location);

        public Transaction[] GetGenesisTransactions(int chainId)
        {
            var transactions = new List<Transaction>();
            transactions.Add(GetTransactionForDeployment(chainId, typeof(BasicContractZero)));
            transactions.Add(GetTransactionForDeployment(chainId, typeof(AElf.Contracts.Consensus.DPoS.Contract)));
            // TODO: Add initialize transactions
            return transactions.ToArray();
        }

        private Transaction GetTransactionForDeployment(int chainId, Type contractType)
        {
            var zeroAddress = Address.BuildContractAddress(chainId, 0);
            var code = File.ReadAllBytes(contractType.Assembly.Location);
            return new Transaction()
            {
                From = zeroAddress,
                To = zeroAddress,
                MethodName = nameof(ISmartContractZero.DeploySmartContract),
                // TODO: change cagtegory to 0
                Params = ByteString.CopyFrom(ParamsPacker.Pack(2, code))
            };
        }
    }
}