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

namespace AElf.OS
{
    //TODO: remove it
    public class InitChainHelper
    {
        public static Transaction[] GetGenesisTransactions(int chainId, Address account, Address tokenAddress)
        {
            var transactions = new List<Transaction>
            {
                GetTransactionForTokenInitialize(chainId, account, tokenAddress)
            };

            return transactions.ToArray();
        }

        public static Transaction GetTransactionForTokenInitialize(int chainId, Address account, Address tokenAddress)
        {
            return new Transaction()
            {
                From = account,
                To = tokenAddress,
                MethodName = "Initialize",
                Params = ByteString.CopyFrom(ParamsPacker.Pack("ELF", "ELF_Token", 100000, 8))
            };
        }
    }
}