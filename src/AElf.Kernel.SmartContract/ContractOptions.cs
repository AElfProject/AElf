namespace AElf.Kernel.SmartContract
{
    public class ContractOptions
    {
        public bool ContractDeploymentAuthorityRequired { get; set; } = true;
        public int GenesisOwnerReleaseThreshold { get; set; } = 6666; // 2/3 for default genesis owner
    }
}