using Acs4;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public class MiningPermissionValidationProvider : IHeaderInformationValidationProvider
    {
        public ValidationResult ValidateHeaderInformation(ConsensusValidationContext validationContext)
        {
            var validationResult = new ValidationResult();
            // Is sender in miner list?
            if (!validationContext.BaseRound.IsInMinerList(validationContext.Pubkey))
            {
                validationResult.Message = $"Sender {validationContext.Pubkey} is not a miner.";
                return validationResult;
            }

            validationResult.Success = true;
            return validationResult;
        }
    }
}