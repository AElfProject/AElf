using System.Collections.Generic;
using AElf.Kernel.SmartContract.Application;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.ExecutionPluginForAcs1.Tests
{
    public class SystemTransactionMethodNameListProvider : ISystemTransactionMethodNameListProvider, ITransientDependency
    {
        public List<string> GetSystemTransactionMethodNameList()
        {
            return new List<string>
            {
                "InitialAElfConsensusContract",
                "FirstRound",
                "NextRound",
                "NextTerm",
                "UpdateValue",
                "UpdateTinyBlockInformation",
                "ClaimTransactionFees",
                "DonateResourceToken",
                "RecordCrossChainData",
                
                //acs5 check tx
                "CheckThreshold",
                //acs8 check tx
                "CheckResourceToken",
                "ChargeResourceToken",
                //genesis deploy
                "DeploySmartContract",
                "DeploySystemSmartContract"
            };
        }
    }
}