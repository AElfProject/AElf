using System.Linq;
using AElf.Kernel.Configuration;
using AElf.Kernel.SmartContract;
using Google.Protobuf;

namespace AElf.Kernel.CodeCheck.Infrastructure;

public class RequiredAcsProvider : IRequiredAcsProvider
{
    private const string RequiredAcsInContractsConfigurationName = "RequiredAcsInContracts";
    private readonly IConfigurationService _configurationService;

    public RequiredAcsProvider(IConfigurationService configurationService)
    {
        _configurationService = configurationService;
    }

    public async Task<RequiredAcs> GetRequiredAcsInContractsAsync(Hash blockHash, long blockHeight)
    {
        var chainContext = new ChainContext
        {
            BlockHash = blockHash,
            BlockHeight = blockHeight
        };

        var returned =
            await _configurationService.GetConfigurationDataAsync(
                RequiredAcsInContractsConfigurationName, chainContext);

        var requiredAcsInContracts = new RequiredAcsInContracts();
        requiredAcsInContracts.MergeFrom(returned);
        return new RequiredAcs
        {
            AcsList = requiredAcsInContracts.AcsList.ToList(),
            RequireAll = requiredAcsInContracts.RequireAll
        };
    }
}