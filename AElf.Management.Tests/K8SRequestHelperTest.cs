using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AElf.Management.Helper;
using k8s;
using k8s.Models;
using Microsoft.AspNetCore.JsonPatch;
using Xunit;

namespace AElf.Management.Tests
{
    public class K8SRequestHelperTest
    {
        [Fact(Skip = "require aws account")]
        //[Fact]
        public void GetClientTest()
        {
            var name = "0x7ef9c60a1283681b895dbeb1f28c0d0d8559";
            var body = new V1Namespace
            {
                Metadata = new V1ObjectMeta
                {
                    Name = name
                }
            };

            K8SRequestHelper.GetClient().CreateNamespace(body);
            var space1 = K8SRequestHelper.GetClient().ListNamespace();
            Assert.True(space1.Items.Select(n => n.Metadata.Name == name).Any());
                
            K8SRequestHelper.GetClient().DeleteNamespace(new V1DeleteOptions(), name);
            var space2 = K8SRequestHelper.GetClient().ListNamespace();
            Assert.False(space2.Items.Select(n => n.Metadata.Name == name).Any());
        }

        [Fact]
        public void Test()
        {
            K8SRequestHelper.GetClient().DeleteCollectionNamespacedPod("0xef23b649fcd3fd35b8389b2aec3da6fa51e9-1", labelSelector: "name=deploy-launcher");
        }
    }
}