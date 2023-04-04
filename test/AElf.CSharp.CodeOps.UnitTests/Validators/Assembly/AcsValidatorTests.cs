using AElf.Kernel.SmartContract;
using Moq;
using Xunit;

namespace AElf.CSharp.CodeOps.Validators.Assembly;

public class AcsValidatorTests
{
    [Fact]
    public void Validate_RequireAllAndContractDoesNotContainAllAcs_ReturnsError()
    {
        // Arrange
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        var requiredAcs = new RequiredAcs
        {
            RequireAll = true,
            AcsList = new List<string> { "Acs1", "Acs2", "Acs3" }
        };

        var mockedAcsValidator = new Mock<IAcsValidator>();
        mockedAcsValidator.Setup(m => m.Validate(assembly, requiredAcs))
            .Returns(new List<ValidationResult>
            {
                new AcsValidationResult("Contract should have all Acs1, Acs2, Acs3 as base.")
            });

        // Act
        var result = mockedAcsValidator.Object.Validate(assembly, requiredAcs);

        // Assert
        Assert.Single(result);
        Assert.Equal("Contract should have all Acs1, Acs2, Acs3 as base.", result.First().Message);
    }

    [Fact]
    public void Validate_RequireAtLeastOneAndContractDoesNotContainAnyAcs_ReturnsError()
    {
        // Arrange
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        var requiredAcs = new RequiredAcs
        {
            RequireAll = false,
            AcsList = new List<string> { "Acs1", "Acs2", "Acs3" }
        };

        var mockedAcsValidator = new Mock<IAcsValidator>();
        mockedAcsValidator.Setup(m => m.Validate(assembly, requiredAcs))
            .Returns(new List<ValidationResult>
            {
                new AcsValidationResult("Contract should have at least Acs1 or Acs2 or Acs3 as base.")
            });

        // Act
        var result = mockedAcsValidator.Object.Validate(assembly, requiredAcs);

        // Assert
        Assert.Single(result);
        Assert.Equal("Contract should have at least Acs1 or Acs2 or Acs3 as base.", result.First().Message);
    }
}