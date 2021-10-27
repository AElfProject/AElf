using AElf.Standards.ACS4;

// ReSharper disable once CheckNamespace
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
            if (!validationContext.BaseRound.RealTimeMinersInformation.Keys.Contains(validationContext.SenderPubkey))
            {
                validationResult.Message = $"Sender {validationContext.SenderPubkey} is not a miner.";
                return validationResult;
            }

            validationResult.Success = true;
            return validationResult;
        }
    }
}