using Xunit;

namespace AElf.CSharp.CodeOps.Validators.Whitelist;

public class NamespaceRuleTests
    {
        [Fact]
        public void ConstructorTest()
        {
            var namespaceRule = new NamespaceRule("TestNamespace", Permission.Allowed);

            Assert.Equal("TestNamespace", namespaceRule.Name);
            Assert.Equal(Permission.Allowed, namespaceRule.Permission);
            Assert.Empty(namespaceRule.Types);
        }

        [Fact]
        public void AddTypeByTypeTest()
        {
            var namespaceRule = new NamespaceRule("TestNamespace", Permission.Allowed);
            namespaceRule.Type(typeof(DummyClass), Permission.Denied);

            Assert.Single(namespaceRule.Types);
            Assert.True(namespaceRule.Types.ContainsKey("DummyClass"));
            Assert.Equal(Permission.Denied, namespaceRule.Types["DummyClass"].Permission);
        }

        [Fact]
        public void AddTypeByNameTest()
        {
            var namespaceRule = new NamespaceRule("TestNamespace", Permission.Allowed);
            namespaceRule.Type("DummyClass", Permission.Denied);

            Assert.Single(namespaceRule.Types);
            Assert.True(namespaceRule.Types.ContainsKey("DummyClass"));
            Assert.Equal(Permission.Denied, namespaceRule.Types["DummyClass"].Permission);
        }

        [Fact]
        public void AddTypeWithRulesTest()
        {
            var namespaceRule = new NamespaceRule("TestNamespace", Permission.Allowed);
            namespaceRule.Type("DummyClass", Permission.Denied, typeRule =>
            {
                typeRule.Member("TestMethod", Permission.Allowed);
            });

            Assert.Single(namespaceRule.Types);
            Assert.True(namespaceRule.Types.ContainsKey("DummyClass"));
            Assert.Equal(Permission.Denied, namespaceRule.Types["DummyClass"].Permission);

            var typeRule = namespaceRule.Types["DummyClass"];
            Assert.Single(typeRule.Members);
            Assert.True(typeRule.Members.ContainsKey("TestMethod"));
            Assert.Equal(Permission.Allowed, typeRule.Members["TestMethod"].Permission);
        }


        private class DummyClass
        {   
            public void TestMethod() { }
        }
    }