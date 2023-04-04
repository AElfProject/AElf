using Xunit;

namespace AElf.CSharp.CodeOps.Validators.Whitelist;

public class TypeRuleTests
{
    [Fact]
    public void TypeRuleConstructorTest()
    {
        var typeRule = new TypeRule("DummyClass", Permission.Denied);

        Assert.Equal("DummyClass", typeRule.Name);
        Assert.Equal(Permission.Denied, typeRule.Permission);
        Assert.Empty(typeRule.Members);
    }

    [Fact]
    public void AddMemberTest()
    {
        var typeRule = new TypeRule("DummyClass", Permission.Denied);
        typeRule.Member("TestMethod", Permission.Allowed);

        Assert.Single(typeRule.Members);
        Assert.True(typeRule.Members.ContainsKey("TestMethod"));
        Assert.Equal(Permission.Allowed, typeRule.Members["TestMethod"].Permission);
    }

    [Fact]
    public void AddConstructorTest()
    {
        var typeRule = new TypeRule("DummyClass", Permission.Denied);
        typeRule.Constructor(Permission.Allowed);

        Assert.Single(typeRule.Members);
        Assert.True(typeRule.Members.ContainsKey(".ctor"));
        Assert.Equal(Permission.Allowed, typeRule.Members[".ctor"].Permission);
    }
}