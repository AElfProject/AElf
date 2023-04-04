using Xunit;

namespace AElf.CSharp.CodeOps.Validators.Whitelist;

public class WhitelistProviderTests
{
    [Fact]
    public void GetWhitelist_NotNull_Test()
    {
        var provider = new WhitelistProvider();
        var whitelist = provider.GetWhitelist();

        Assert.NotNull(whitelist);
    }

    [Fact]
    public void GetWhitelist_Singleton_Test()
    {
        var provider = new WhitelistProvider();
        var whitelist1 = provider.GetWhitelist();
        var whitelist2 = provider.GetWhitelist();

        Assert.Same(whitelist1, whitelist2);
    }
}

public class SystemContractWhitelistProviderTests
{
    [Fact]
    public void GetWhitelist_NotNull_Test()
    {
        var provider = new SystemContractWhitelistProvider();
        var whitelist = provider.GetWhitelist();

        Assert.NotNull(whitelist);
    }

    [Fact]
    public void GetWhitelist_Singleton_Test()
    {
        var provider = new SystemContractWhitelistProvider();
        var whitelist1 = provider.GetWhitelist();
        var whitelist2 = provider.GetWhitelist();

        Assert.Same(whitelist1, whitelist2);
    }
}