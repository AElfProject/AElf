using Acs4;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public class MiningPermissionValidationProvider : IHeaderInformationValidationProvider
    {
        /// <summary>
        /// This validation will based on current round information stored in StateDb.
        /// Simply check keys of RealTimeMinersInformation should be enough.
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public ValidationResult ValidateHeaderInformation(ConsensusValidationContext validationContext)
        {
            var validationResult = new ValidationResult();
            if (!validationContext.BaseRound.RealTimeMinersInformation.Keys.Contains(validationContext.Pubkey))
            {
                validationResult.Message = $"Sender {validationContext.Pubkey} is not a miner.";
                return validationResult;
            }

            validationResult.Success = true;
            return validationResult;
        }
    }
}