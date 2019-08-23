using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.TestKit;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;

namespace AElf.Contracts.Economic.TestBase
{
    public partial class EconomicContractsTestBase : ContractTestBase<EconomicContractsTestModule>
    {
        protected IBlockTimeProvider BlockTimeProvider =>
            Application.ServiceProvider.GetRequiredService<IBlockTimeProvider>();
        
        protected Timestamp StartTimestamp => TimestampHelper.GetUtcNow();
        
        protected ECKeyPair BootMinerKeyPair => SampleECKeyPairs.KeyPairs[0];

        protected Address BootMinerAddress => Address.FromPublicKey(BootMinerKeyPair.PublicKey);

        protected Address ConnectorManagerAddress => Address.FromPublicKey(BootMinerKeyPair.PublicKey);
        protected static List<ECKeyPair> InitialCoreDataCenterKeyPairs =>
            SampleECKeyPairs.KeyPairs.Take(EconomicContractsTestConstants.InitialCoreDataCenterCount).ToList();

        protected static List<ECKeyPair> CoreDataCenterKeyPairs =>
            SampleECKeyPairs.KeyPairs.Skip(EconomicContractsTestConstants.InitialCoreDataCenterCount)
                .Take(EconomicContractsTestConstants.CoreDataCenterCount).ToList();

        protected static List<ECKeyPair> ValidationDataCenterKeyPairs =>
            SampleECKeyPairs.KeyPairs
                .Skip(EconomicContractsTestConstants.InitialCoreDataCenterCount +
                      EconomicContractsTestConstants.CoreDataCenterCount)
                .Take(EconomicContractsTestConstants.ValidateDataCenterCount).ToList();

        protected static List<ECKeyPair> ValidationDataCenterCandidateKeyPairs =>
            SampleECKeyPairs.KeyPairs
                .Skip(EconomicContractsTestConstants.InitialCoreDataCenterCount +
                      EconomicContractsTestConstants.CoreDataCenterCount +
                      EconomicContractsTestConstants.ValidateDataCenterCount)
                .Take(EconomicContractsTestConstants.ValidateDataCenterCandidateCount).ToList();

        protected static List<ECKeyPair> VoterKeyPairs =>
            SampleECKeyPairs.KeyPairs
                .Skip(EconomicContractsTestConstants.InitialCoreDataCenterCount +
                      EconomicContractsTestConstants.CoreDataCenterCount +
                      EconomicContractsTestConstants.ValidateDataCenterCount +
                      EconomicContractsTestConstants.ValidateDataCenterCandidateCount)
                .Take(EconomicContractsTestConstants.VoterCount).ToList();
    }
}