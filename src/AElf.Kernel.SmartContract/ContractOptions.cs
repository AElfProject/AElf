namespace AElf.Kernel.SmartContract
{
    public class ContractOptions
    {
        public bool ContractDeploymentAuthorityRequired { get; set; } = true;
        public string GenesisContractDir { get; set; }
    }
}