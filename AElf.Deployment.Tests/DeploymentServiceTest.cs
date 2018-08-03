using System;
using System.Collections.Concurrent;
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
            var pods = K8SRequestHelper.ListNamespacedPod("default");

            
            var body = new Extensionsv1beta1Deployment();
            body.ApiVersion = "extensions/v1beta1";
            body.Kind = "Deployment";

            body.Metadata = new V1ObjectMeta();
            body.Metadata.Name = "worker-test";
            body.Metadata.Labels = new Dictionary<string, string>();
            body.Metadata.Labels.Add("name", "worker-test");

            body.Spec = new Extensionsv1beta1DeploymentSpec();
            body.Spec.Selector = new V1LabelSelector();
            body.Spec.Selector.MatchLabels = body.Metadata.Labels;

            body.Spec.Replicas = 2;

            body.Spec.Template = new V1PodTemplateSpec();
            body.Spec.Template.Metadata = new V1ObjectMeta();
            body.Spec.Template.Metadata.Labels = body.Metadata.Labels;

            body.Spec.Template.Spec = new V1PodSpec();

            body.Spec.Template.Spec.Containers = new List<V1Container>();
            var container1 = new V1Container();
            container1.Name = "worker-test";
            container1.Image = "aelf/node:worker";
            container1.Ports = new List<V1ContainerPort>();
            container1.Ports.Add(new V1ContainerPort(32551));

            container1.Env = new List<V1EnvVar>();
            var env1 = new V1EnvVar();
            env1.Name = "POD_IP";
            env1.ValueFrom = new V1EnvVarSource();
            env1.ValueFrom.FieldRef = new V1ObjectFieldSelector();
            env1.ValueFrom.FieldRef.FieldPath = "status.podIP";
            container1.Env.Add(env1);

            container1.Args = new List<string>();
            container1.Args.Add("--actor.host");
            container1.Args.Add("$(POD_IP)");
            container1.Args.Add("--actor.port");
            container1.Args.Add("32551");

            container1.VolumeMounts = new List<V1VolumeMount>();
            var volumeMount1 = new V1VolumeMount();
            volumeMount1.MountPath = "/app/aelf/config";
            volumeMount1.Name = "config";
            container1.VolumeMounts.Add(volumeMount1);

            body.Spec.Template.Spec.Containers.Add(container1);

            body.Spec.Template.Spec.Volumes = new List<V1Volume>();
            var volume1 = new V1Volume();
            volume1.Name = "config";
            volume1.ConfigMap = new V1ConfigMapVolumeSource();
            volume1.ConfigMap.Name = "aelf-config";
            body.Spec.Template.Spec.Volumes.Add(volume1);

            var namespaceParameter = "default";

            var result = K8SRequestHelper.CreateNamespacedDeployment3(body, namespaceParameter);
            
            
        }

        [Fact]
        public void PatchDeploymentTest()
        {
            try
            {
                var patch = new JsonPatchDocument<Extensionsv1beta1Deployment>();
                patch.Replace(e => e.Spec.Replicas, 5);
            
                var body = new V1Patch(patch);

                K8SRequestHelper.PatchNamespacedDeployment3(body,"worker-test", "default");
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
                var body = new V1DeleteOptions();
                body.PropagationPolicy = "Foreground";
                var result = K8SRequestHelper.DeleteNamespacedDeployment3(body, "worker", "default");
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
                var body = new V1Namespace
                {
                    Metadata = new V1ObjectMeta
                    {
                        Name = "test"
                    }
                };
            
                var result = K8SRequestHelper.CreateNamespace(body);
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
                var status = K8SRequestHelper.DeleteNamespace(new V1DeleteOptions(), "test");
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

                K8SRequestHelper.CreateNamespacedConfigMap(body, "default");

                var configList1 = K8SRequestHelper.ListNamespacedConfigMap("default");

                var patch = new JsonPatchDocument<V1ConfigMap>();
                patch.Replace(e => e.Data, new Dictionary<string, string> {{"3", "3"}});

                var bodyPatch = new V1Patch(patch);
                K8SRequestHelper.PatchNamespacedConfigMap(bodyPatch, "test", "default");

                var configList2 = K8SRequestHelper.ListNamespacedConfigMap("default");

                K8SRequestHelper.DeleteNamespacedConfigMap(new V1DeleteOptions(), "test", "default");

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        
        [Fact]
        public void ServiceTest()
        {
            try
            {
                var body = new V1Service();
                body.Metadata=new V1ObjectMeta();
                body.Metadata.Name = "managertest-service";
                body.Metadata.Labels=new Dictionary<string, string>();
                body.Metadata.Labels.Add("name","managertest-service");
                
                body.Spec=new V1ServiceSpec();
                body.Spec.Ports = new List<V1ServicePort>();
                body.Spec.Ports.Add(new V1ServicePort(4053, null, null, "TCP", 4053));
                body.Spec.Selector=new Dictionary<string, string>();
                body.Spec.Selector.Add("name","managertest");

                body.Spec.ClusterIP = "None";
                
                K8SRequestHelper.CreateNamespacedService(body, "default");

                var sercices = K8SRequestHelper.ListNamespacedService("default");

                K8SRequestHelper.DeleteNamespacedService(new V1DeleteOptions(), "managertest-service", "default");
                
                var sercices2 = K8SRequestHelper.ListNamespacedService("default");

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
                var body = new V1beta1StatefulSet();
                body.Metadata=new V1ObjectMeta();
                body.Metadata.Name = "managertest";
                body.Metadata.Labels=new Dictionary<string, string>();
                body.Metadata.Labels.Add("name", "managertest");
                
                body.Spec=new V1beta1StatefulSetSpec();
                body.Spec.Selector=new V1LabelSelector();
                body.Spec.Selector.MatchExpressions=new List<V1LabelSelectorRequirement>();
                body.Spec.Selector.MatchExpressions.Add(new V1LabelSelectorRequirement("name", "In", new List<string> {"managertest"}));
                body.Spec.ServiceName = "managertest-service";
                body.Spec.Replicas = 1;
                body.Spec.Template=new V1PodTemplateSpec();
                body.Spec.Template.Metadata=new V1ObjectMeta();
                body.Spec.Template.Metadata.Labels=new Dictionary<string, string>();
                body.Spec.Template.Metadata.Labels.Add("name", "managertest");
                body.Spec.Template.Spec = new V1PodSpec();
                body.Spec.Template.Spec.Containers=new List<V1Container>();
                var container1 = new V1Container();
                container1.Name = "managertest";
                container1.Image = "aelf/node:manager";
                container1.Ports =new List<V1ContainerPort>();
                container1.Ports.Add(new V1ContainerPort(4053));
                container1.Env=new List<V1EnvVar>();
                var V1EnvVar1 = new V1EnvVar();
                V1EnvVar1.Name = "POD_NAME";
                V1EnvVar1.ValueFrom = new V1EnvVarSource();
                V1EnvVar1.ValueFrom.FieldRef = new V1ObjectFieldSelector("metadata.name");
                container1.Env.Add(V1EnvVar1);
                container1.Args = new List<string> {"--actor.host", "$(POD_NAME).manager-service", "--actor.port", "4053"};
                container1.VolumeMounts=new List<V1VolumeMount>();
                container1.VolumeMounts.Add(new V1VolumeMount("/app/aelf/config","config"));
                body.Spec.Template.Spec.Containers.Add(container1);
                body.Spec.Template.Spec.Volumes=new List<V1Volume>();
                var v1Volume1 = new V1Volume();
                v1Volume1.Name = "config";
                v1Volume1.ConfigMap=new V1ConfigMapVolumeSource();
                v1Volume1.ConfigMap.Name = "aelf-config";
                body.Spec.Template.Spec.Volumes.Add(v1Volume1);

                K8SRequestHelper.CreateNamespacedStatefulSet1(body, "default");

                var sets = K8SRequestHelper.ListNamespacedStatefulSet1("default");
        
                var deleteBody = new V1DeleteOptions();
                deleteBody.PropagationPolicy = "Foreground";
                K8SRequestHelper.DeleteNamespacedStatefulSet1(deleteBody, "managertest", "default");

                var sets2 = K8SRequestHelper.ListNamespacedStatefulSet1("default");

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}