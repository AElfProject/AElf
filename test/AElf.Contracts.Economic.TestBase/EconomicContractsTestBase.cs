using System.Collections.Generic;
using System.Linq;
using AElf.ContractTestKit;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;

namespace AElf.Contracts.Economic.TestBase;

public partial class EconomicContractsTestBase : ContractTestBase<EconomicContractsTestModule>
{
    protected IBlockTimeProvider BlockTimeProvider =>
        Application.ServiceProvider.GetRequiredService<IBlockTimeProvider>();
    
    protected IBlockchainService BlockchainService =>
        Application.ServiceProvider.GetRequiredService<IBlockchainService>();

    protected Timestamp StartTimestamp => TimestampHelper.GetUtcNow();

    protected ECKeyPair BootMinerKeyPair => Accounts[0].KeyPair;

    protected Address BootMinerAddress => Address.FromPublicKey(BootMinerKeyPair.PublicKey);

    protected List<ECKeyPair> InitialCoreDataCenterKeyPairs =>
        Accounts.Take(EconomicContractsTestConstants.InitialCoreDataCenterCount).Select(a => a.KeyPair).ToList();

    protected List<ECKeyPair> CoreDataCenterKeyPairs =>
        Accounts.Skip(EconomicContractsTestConstants.InitialCoreDataCenterCount)
            .Take(EconomicContractsTestConstants.CoreDataCenterCount).Select(a => a.KeyPair).ToList();

    protected List<ECKeyPair> ValidationDataCenterKeyPairs =>
        Accounts
            .Skip(EconomicContractsTestConstants.InitialCoreDataCenterCount +
                  EconomicContractsTestConstants.CoreDataCenterCount)
            .Take(EconomicContractsTestConstants.ValidateDataCenterCount).Select(a => a.KeyPair).ToList();

    protected List<ECKeyPair> ValidationDataCenterCandidateKeyPairs =>
        Accounts
            .Skip(EconomicContractsTestConstants.InitialCoreDataCenterCount +
                  EconomicContractsTestConstants.CoreDataCenterCount +
                  EconomicContractsTestConstants.ValidateDataCenterCount)
            .Take(EconomicContractsTestConstants.ValidateDataCenterCandidateCount).Select(a => a.KeyPair).ToList();

    protected List<ECKeyPair> VoterKeyPairs =>
        Accounts
            .Skip(EconomicContractsTestConstants.InitialCoreDataCenterCount +
                  EconomicContractsTestConstants.CoreDataCenterCount +
                  EconomicContractsTestConstants.ValidateDataCenterCount +
                  EconomicContractsTestConstants.ValidateDataCenterCandidateCount)
            .Take(EconomicContractsTestConstants.VoterCount).Select(a => a.KeyPair).ToList();
}