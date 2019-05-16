using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.KernelAccount
{
    public interface ISmartContractZero : ISmartContract
    {
        ContractInfo GetContractInfo(Address address);
        Address DeploySmartContract(ContractDeploymentInput input);

        Address DeploySystemSmartContract(SystemContractDeploymentInput input);

        Address GetContractAddressByName(Hash name);
        SmartContractRegistration GetSmartContractRegistrationByAddress(Address address);
        Empty Initialize(ContractZeroInitializationInput input);
    }
}