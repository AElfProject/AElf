using AElf.Types;

namespace AElf.Contracts.Whitelist.Extensions;

public static class WhitelistExtensions
{
    public static Hash CalculateExtraInfoId(this Hash whitelistId, Hash projectId, string tagName)
    {
        return HashHelper.ComputeFrom($"{whitelistId}{projectId}{tagName}");
    }
}