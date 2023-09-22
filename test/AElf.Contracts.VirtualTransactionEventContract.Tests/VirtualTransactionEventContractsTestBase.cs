using System.Threading.Tasks;
using AElf.ContractTestBase;
using AElf.ContractTestBase.ContractTestKit;
using AElf.Cryptography.ECDSA;
using AElf.Types;
using Volo.Abp.Threading;

namespace AElf.Contracts.TestContract.VirtualTransactionEvent;

public class VirtualTransactionEventContractsTestBase : ContractTestBase<VirtualTransactionEventContractsTestModule>
{
    protected Hash VirtualTransactionContractName => HashHelper.ComputeFrom("AElf.TestContractNames.VirtualTransactionEvent");
    protected Hash AContractName => HashHelper.ComputeFrom("AElf.TestContractNames.A");

    protected Address VirtualTransactionContractAddress { get; set; }
    protected Address AContractAddress { get; set; }

    internal VirtualTransactionEventContractContainer.VirtualTransactionEventContractStub VirtualTransactionEventContractStub { get; set; }

    protected ECKeyPair DefaultKeyPair => Accounts[0].KeyPair;
    protected Address DefaultSender => Accounts[0].Address;

    public VirtualTransactionEventContractsTestBase()
    {
        VirtualTransactionContractAddress = SystemContractAddresses[VirtualTransactionContractName];
        VirtualTransactionEventContractStub = GetTester<VirtualTransactionEventContractContainer.VirtualTransactionEventContractStub>(
            VirtualTransactionContractAddress, DefaultKeyPair);
        AContractAddress = SystemContractAddresses[AContractName];

    }
}