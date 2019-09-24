using System.Collections.Generic;
using Acs4;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public class HeaderInformationValidationService
    {
        private readonly IEnumerable<IHeaderInformationValidationProvider> _headerInformationValidationProviders;

        public HeaderInformationValidationService(
            IEnumerable<IHeaderInformationValidationProvider> headerInformationValidationProviders)
        {
            _headerInformationValidationProviders = headerInformationValidationProviders;
        }

        public ValidationResult ValidateInformation(ConsensusValidationContext validationContext)
        {
            foreach (var headerInformationValidationProvider in _headerInformationValidationProviders)
            {
                var result =
                    headerInformationValidationProvider.ValidateHeaderInformation(validationContext);
                if (!result.Success)
                {
                    return result;
                }
            }

            return new ValidationResult {Success = true};
        }
    }
}