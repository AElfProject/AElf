using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestContract.MockParliament;

public class Contract : MockParliamentContractContainer.MockParliamentContractBase
{
    public override Address GetDefaultOrganizationAddress(Empty input)
    {
        return State.DefaultOrganizationAddress.Value ?? new Address();
    }

    public override Empty Initialize(InitializeInput input)
    {
        State.DefaultOrganizationAddress.Value = input.PrivilegedProposer;

        return new Empty();
    }
}