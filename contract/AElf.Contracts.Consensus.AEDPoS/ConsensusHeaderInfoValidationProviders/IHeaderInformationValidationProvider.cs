using Acs4;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public interface IHeaderInformationValidationProvider
    {
        //ValidationResult ValidateHeaderInformation(Round baseRound, Dictionary<long, Round> rounds);
        ValidationResult ValidateHeaderInformation(ConsensusValidationContext validationContext);
    }
}