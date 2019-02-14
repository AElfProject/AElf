using System.Collections.Generic;
using AElf.Common;
using AElf.Configuration.Config.Chain;
using AElf.Kernel;
using AElf.Kernel.Types;
using AElf.Types.CSharp;
using Google.Protobuf;

namespace AElf.Miner.Miner
{
    public class FeeClaimingTransactionGenerator : ISystemTransactionGenerator
    {
        public void GenerateTransactions(Address from, ulong preBlockHeight, ulong refBlockHeight, 
            byte[] refBlockPrefix, ref List<Transaction> generatedTransactions)
        {
            if (UnitTestDetector.IsInUnitTest) return;
            var tx = new Transaction()
            {
                From = @from,
                To = ContractHelpers.GetTokenContractAddress(ChainConfig.Instance.ChainId.ConvertBase58ToChainId()),
                MethodName = "ClaimTransactionFees",
                RefBlockNumber = refBlockHeight,
                RefBlockPrefix = ByteString.CopyFrom(refBlockPrefix),
                Params = ByteString.CopyFrom(ParamsPacker.Pack(preBlockHeight))
            };
            generatedTransactions.Add(tx);
        }
    }
}