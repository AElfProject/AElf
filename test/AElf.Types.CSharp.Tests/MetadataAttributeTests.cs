using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using AElf.Common;
using AElf.Kernel;
using AElf.Types.CSharp.MetadataAttribute;
using Org.BouncyCastle.Asn1.X509.SigI;
using Shouldly;
using Xunit;

namespace AElf.Types.CSharp
{
    public class MetadataAttributeTests : TypesCSharpTestBase
    {
        [Fact]
        public void SmartContractFieldDataAttribute_Test()
        {
            var prop = typeof(MetadataDemo).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => new
                {
                    PropertyName = p.Name,
                    AttributeData = p.GetCustomAttribute(typeof(SmartContractFieldDataAttribute), false)
                        .As<SmartContractFieldDataAttribute>()
                }).ToList();
            
            //Assert
            prop.Count.ShouldBe(3);
            
            prop[0].PropertyName.ShouldBe(nameof(MetadataDemo.Property1));
            prop[0].AttributeData.ShouldBe(new SmartContractFieldDataAttribute("Property1", DataAccessMode.AccountSpecific));
            
            prop[1].PropertyName.ShouldBe(nameof(MetadataDemo.Property2));
            prop[1].AttributeData.ShouldBe(new SmartContractFieldDataAttribute("Property2", DataAccessMode.ReadOnlyAccountSharing));

            prop[2].PropertyName.ShouldBe(nameof(MetadataDemo.Property3));
            prop[2].AttributeData.ShouldBe(new SmartContractFieldDataAttribute("Property3", DataAccessMode.ReadWriteAccountSharing));
        }
        
        public class MetadataDemo
        {
            [SmartContractFieldData("Property1", DataAccessMode.AccountSpecific)]
            public Address Property1 { get; set; }

            [SmartContractFieldData("Property2", DataAccessMode.ReadOnlyAccountSharing)]
            public Address Property2 { get; set; }
            
            [SmartContractFieldData("Property3", DataAccessMode.ReadWriteAccountSharing)]
            public Address Property3 { get; set; }
        }
    }
}