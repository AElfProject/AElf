using AElf.SolidityContract;
using AElf.Types;

namespace AElf.Contracts.Genesis;

public partial class BasicContractZero
{
    public override Address DeploySoliditySmartContract(DeploySoliditySmartContractInput input)
    {
        AssertSenderAddressWith(Context.Self);
        var address =
            DeploySmartContract(null, input.Category, input.Code.ToByteArray(), false,
                Context.Sender, false, input.Parameter);
        return address;
    }

    public override Address GetContractAddressByCodeHash(Hash input)
    {
        return State.CodeHashToAddressMap[input];
    }
}