using AElf.Standards.ACS4;

// ReSharper disable once CheckNamespace
namespace AElf.Contracts.Consensus.AEDPoS
{
    public interface IHeaderInformationValidationProvider
    {
        ValidationResult ValidateHeaderInformation(ConsensusValidationContext validationContext);
    }
}