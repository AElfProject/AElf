using System;
using System.Collections.Generic;
using AElf.Deployment.Helper;
using k8s.Models;
using Xunit;
using Microsoft.AspNetCore.JsonPatch;


namespace AElf.Deployment.Tests
{
    public class DeploymentServiceTest
    {
        [Fact]
        public void CreateDeploymentTest()
        {
            
            var service = new DeploymentService();

            var pods = service.GetPods();
            
            service.CreateDeployment();
        }
        
//        [Fact]
           //        public void ScaleDeploymentTest()
           //        {
           //            try
           //            {
           //            
           //                var service = new DeploymentService();
           //                var pods = service.GetPods();
           //            
           //                Assert.True(pods.Items.Count==4);
           //            
           //                service.Scale();
           //            
           //                var podsAfter = service.GetPods();
           //            
           //                Assert.True(podsAfter.Items.Count==5);
           //            }
           //            catch (Exception e)
           //            {
           //                Console.WriteLine(e);
           //                throw;
           //            }
           //        }
        
        [Fact]
        public void ReployDeploymentTest()
        {
            try
            {
                var service = new DeploymentService();
                var pods = service.GetPods();
            
                Assert.True(pods.Items.Count==4);
            
                service.ReplaceDeployment();
            
                var podsAfter = service.GetPods();
            
                Assert.True(podsAfter.Items.Count==5);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        
        [Fact]
        public void PatchDeploymentTest()
        {
            try
            {
                var service = new DeploymentService();
                var pods = service.GetPods();
            
                Assert.True(pods.Items.Count==5);
            
                service.PatchDepoyment();
            
                var podsAfter = service.GetPods();
            
                Assert.True(podsAfter.Items.Count==6);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        
        [Fact]
        public void DeleteDeploymentTest()
        {
            try
            {
                var service = new DeploymentService(); 
                var pods = service.GetPods();
            
                Assert.True(pods.Items.Count==7);
            
                service.DeleteDeployment();
            
                var podsAfter = service.GetPods();
            
                Assert.True(podsAfter.Items.Count==2);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        
        [Fact]
        public void CreateNamespaceTest()
        {
            try
            {
                var service = new DeploymentService();
                var pods = service.GetPods();
            
                Assert.True(pods!=null);
            
                service.CreateNamespace();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        
        [Fact]
        public void DeleteNamespaceTest()
        {
            try
            {
                var service = new DeploymentService();
                var pods = service.ListNamespace();
            
                Assert.True(pods!=null);
            
                service.DeleteNamespace();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        
        [Fact]
        public void ConfigMapTest()
        {
            try
            {
                var body = new V1ConfigMap();
                body.ApiVersion = V1ConfigMap.KubeApiVersion;
                body.Kind = V1ConfigMap.KubeKind;
                body.Metadata = new V1ObjectMeta();
                body.Metadata.Name = "test";
                body.Metadata.NamespaceProperty = "default";
                body.Data = new Dictionary<string, string>();
                body.Data.Add("1", "1");
                body.Data.Add("2", "2");

                KubernetesHelper.CreateNamespacedConfigMap(body, "default");

                var configList1 = KubernetesHelper.ListNamespacedConfigMap("default");

                var patch = new JsonPatchDocument<V1ConfigMap>();
                patch.Replace(e => e.Data, new Dictionary<string, string> {{"3", "3"}});

                var bodyPatch = new V1Patch(patch);
                KubernetesHelper.PatchNamespacedConfigMap(bodyPatch, "test", "default");

                var configList2 = KubernetesHelper.ListNamespacedConfigMap("default");

                KubernetesHelper.DeleteNamespacedConfigMap(new V1DeleteOptions(), "test", "default");

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        
        [Fact]
        public void StatefullSetTest()
        {
            try
            {
                

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}