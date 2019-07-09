namespace AElf.Kernel.KernelAccount
{
    // Done: move to project
    public interface ISmartContractZero : ISmartContract
    {
        ContractInfo GetContractInfo(Address address);
        Address DeploySmartContract(ContractDeploymentInput input);

        Address DeploySystemSmartContract(SystemContractDeploymentInput input);

        Address GetContractAddressByName(Hash name);
        SmartContractRegistration GetSmartContractRegistrationByAddress(Address address);
    }
}