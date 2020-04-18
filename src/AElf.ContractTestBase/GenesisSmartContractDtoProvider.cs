using System.Collections.Generic;
using System.Linq;
using AElf.Blockchains.ContractInitialization;
using AElf.OS.Node.Application;

namespace AElf.ContractTestBase
{
    public class GenesisSmartContractDtoProvider : IGenesisSmartContractDtoProvider
    {
        private readonly List<IContractInitializationProvider> _contractInitializationProviders;
        private readonly IContractCodeProvider _contractCodeProvider;

        public GenesisSmartContractDtoProvider(
            IServiceContainer<IContractInitializationProvider> contractInitializationProviders,
            IContractCodeProvider contractCodeProvider)
        {
            _contractCodeProvider = contractCodeProvider;
            _contractInitializationProviders = contractInitializationProviders.ToList();
        }

        public IEnumerable<GenesisSmartContractDto> GetGenesisSmartContractDtos()
        {
            var contractCode = _contractCodeProvider.Codes;
            return _contractInitializationProviders.Select(provider => provider.GetGenesisSmartContractDto(contractCode))
                .ToList();
        }
    }
}