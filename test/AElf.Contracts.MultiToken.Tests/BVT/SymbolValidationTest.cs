using System.Text.RegularExpressions;
using Shouldly;
using Xunit;

namespace AElf.Contracts.MultiToken;

public class SymbolValidationTest
{
    private const string RegexPattern = "^[a-zA-Z0-9]+(-[0-9]+)?$";

    [Theory]
    [InlineData("ELF", true)]
    [InlineData("ELF-", false)]
    [InlineData("ABC-123", true)]
    [InlineData("abc-1", true)]
    [InlineData("ABC-ABC", false)]
    [InlineData("ABC--", false)]
    [InlineData("121-1", true)]
    public void SymbolValidation(string symbol, bool isValid)
    {
        Regex.IsMatch(symbol, RegexPattern).ShouldBe(isValid);
    }
}