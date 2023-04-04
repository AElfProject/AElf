using Xunit;

namespace AElf.CSharp.CodeOps.Validators;

public class ValidationResultTests
{
    [Fact]
    public void ValidationResult_ShouldCreateWithMessage()
    {
        var message = "Test message";
        var validationResult = new TestValidationResult(message);

        Assert.Equal(message, validationResult.Message);
    }

    [Fact]
    public void ValidationResult_WithInfo_ShouldSetInfo()
    {
        var validationResult = new TestValidationResult("Test message");
        string referencingMethod = "ReferencingMethod";
        string nm = "Namespace";
        string type = "Type";
        string member = "Member";

        validationResult.WithInfo(referencingMethod, nm, type, member);

        Assert.NotNull(validationResult.Info);
        Assert.Equal(referencingMethod, validationResult.Info.ReferencingMethod);
        Assert.Equal(nm, validationResult.Info.Namespace);
        Assert.Equal(type, validationResult.Info.Type);
        Assert.Equal(member, validationResult.Info.Member);
    }

    [Fact]
    public void Info_ToString_ShouldReturnFormattedString()
    {
        var referencingMethod = "ReferencingMethod";
        var nm = "Namespace";
        var type = "Type";
        var member = "Member";
        var info = new Info(referencingMethod, nm, type, member);

        var expected = $"{referencingMethod} > {nm} | {type} | {member}";
        Assert.Equal(expected, info.ToString());
    }
}

public class TestValidationResult : ValidationResult
{
    public TestValidationResult(string message) : base(message)
    {
    }
}
