using System;
using System.Collections.Generic;
using AElf.Deployment.Helper;
using k8s.Models;
using Microsoft.AspNetCore.JsonPatch;

namespace AElf.Deployment
{
    public class DeploymentService : IDeploymentService
    {
        public void CreateDeployment()
        {
            var body = new Extensionsv1beta1Deployment();
            body.ApiVersion = "extensions/v1beta1";
            body.Kind = "Deployment";
            
            body.Metadata= new V1ObjectMeta();
            body.Metadata.Name = "worker-test";
            body.Metadata.Labels=new Dictionary<string, string>();
            body.Metadata.Labels.Add("name","worker-test");
            
            body.Spec = new Extensionsv1beta1DeploymentSpec();
            body.Spec.Selector=new V1LabelSelector();
            body.Spec.Selector.MatchLabels = body.Metadata.Labels;

            body.Spec.Replicas = 2;
            
            body.Spec.Template = new V1PodTemplateSpec();
            body.Spec.Template.Metadata = new V1ObjectMeta();
            body.Spec.Template.Metadata.Labels = body.Metadata.Labels;
            
            body.Spec.Template.Spec=new V1PodSpec();
            
            body.Spec.Template.Spec.Containers=new List<V1Container>();
            var container1 = new V1Container();
            container1.Name = "worker-test";
            container1.Image = "aelf/node:worker";
            container1.Ports=new List<V1ContainerPort>();
            container1.Ports.Add(new V1ContainerPort(32551));
            
            container1.Env=new List<V1EnvVar>();
            var env1 = new V1EnvVar();
            env1.Name = "POD_IP";
            env1.ValueFrom=new V1EnvVarSource();
            env1.ValueFrom.FieldRef=new V1ObjectFieldSelector();
            env1.ValueFrom.FieldRef.FieldPath = "status.podIP";
            container1.Env.Add(env1);
            
            container1.Args=new List<string>();
            container1.Args.Add("--actor.host");
            container1.Args.Add("$(POD_IP)");
            container1.Args.Add("--actor.port");
            container1.Args.Add("32551");
            
            container1.VolumeMounts=new List<V1VolumeMount>();
            var volumeMount1 = new V1VolumeMount();
            volumeMount1.MountPath = "/app/aelf/config";
            volumeMount1.Name = "config";
            container1.VolumeMounts.Add(volumeMount1);
            
            body.Spec.Template.Spec.Containers.Add(container1);
            
            body.Spec.Template.Spec.Volumes=new List<V1Volume>();
            var volume1 = new V1Volume();
            volume1.Name = "config";
            volume1.ConfigMap=new V1ConfigMapVolumeSource();
            volume1.ConfigMap.Name = "aelf-config";
            body.Spec.Template.Spec.Volumes.Add(volume1);
            
            var namespaceParameter = "default";
            
            var result =  KubernetesHelper.CreateNamespacedDeployment3(body, namespaceParameter);
        }

        public V1PodList GetPods()
        {
            return KubernetesHelper.ListNamespacedPod("default");
        }

//        public void Scale()
//        {
//            var body = new Extensionsv1beta1Scale();
//            body.Kind = "Scale";
//            body.ApiVersion = "extensions/v1beta1";
//            body.Metadata = new V1ObjectMeta();
//            body.Metadata.Name = "worker-test-scale";
//            body.Spec = new Extensionsv1beta1ScaleSpec(3);
//            
//            body.Metadata.OwnerReferences=new List<V1OwnerReference>();
//            var reference = new V1OwnerReference();
//            reference.ApiVersion = "extensions/v1beta1";
//            reference.Kind = "Deployment";
//            reference.Name = "worker-test";
//            reference.Uid = "worker-test";
//            
//            body.Metadata.OwnerReferences.Add(reference);
//            
//            KubernetesHelper.ReplaceNamespacedDeploymentScale3(body, "worker-test-scale", "default");
//        }

        public void ReplaceDeployment()
        {
            var body = new Extensionsv1beta1Deployment();
            body.ApiVersion = "extensions/v1beta1";
            body.Kind = "Deployment";
            
            body.Metadata= new V1ObjectMeta();
            body.Metadata.Name = "worker-test";
            body.Metadata.Labels=new Dictionary<string, string>();
            body.Metadata.Labels.Add("name","worker-test");
            
            body.Spec = new Extensionsv1beta1DeploymentSpec();
            body.Spec.Selector=new V1LabelSelector();
            body.Spec.Selector.MatchLabels = body.Metadata.Labels;

            body.Spec.Replicas = 3;
            
            body.Spec.Template = new V1PodTemplateSpec();
            body.Spec.Template.Metadata = new V1ObjectMeta();
            body.Spec.Template.Metadata.Labels = body.Metadata.Labels;
            
            body.Spec.Template.Spec=new V1PodSpec();
            
            body.Spec.Template.Spec.Containers=new List<V1Container>();
            var container1 = new V1Container();
            container1.Name = "worker-test";
            container1.Image = "aelf/node:worker";
            container1.Ports=new List<V1ContainerPort>();
            container1.Ports.Add(new V1ContainerPort(32551));
            
            container1.Env=new List<V1EnvVar>();
            var env1 = new V1EnvVar();
            env1.Name = "POD_IP";
            env1.ValueFrom=new V1EnvVarSource();
            env1.ValueFrom.FieldRef=new V1ObjectFieldSelector();
            env1.ValueFrom.FieldRef.FieldPath = "status.podIP";
            container1.Env.Add(env1);
            
            container1.Args=new List<string>();
            container1.Args.Add("--actor.host");
            container1.Args.Add("$(POD_IP)");
            container1.Args.Add("--actor.port");
            container1.Args.Add("32551");
            
            container1.VolumeMounts=new List<V1VolumeMount>();
            var volumeMount1 = new V1VolumeMount();
            volumeMount1.MountPath = "/app/aelf/config";
            volumeMount1.Name = "config";
            container1.VolumeMounts.Add(volumeMount1);
            
            body.Spec.Template.Spec.Containers.Add(container1);
            
            body.Spec.Template.Spec.Volumes=new List<V1Volume>();
            var volume1 = new V1Volume();
            volume1.Name = "config";
            volume1.ConfigMap=new V1ConfigMapVolumeSource();
            volume1.ConfigMap.Name = "aelf-config";
            body.Spec.Template.Spec.Volumes.Add(volume1);
            
            var namespaceParameter = "default";

            KubernetesHelper.ReplaceNamespacedDeployment3(body, "worker-test", "default");
        }

        public void PatchDepoyment()
        {
            var patch = new JsonPatchDocument<Extensionsv1beta1Deployment>();
            patch.Replace(e => e.Spec.Replicas, 5);
            
            var body = new V1Patch(patch);

            KubernetesHelper.PatchNamespacedDeployment3(body,"worker-test", "default");
        }

        public void DeleteDeployment()
        {
            var body = new V1DeleteOptions();
            body.PropagationPolicy = "Foreground";
            var result = KubernetesHelper.DeleteNamespacedDeployment3(body, "worker", "default");
        }

        public void CreateNamespace()
        {
            var body = new V1Namespace
            {
                Metadata = new V1ObjectMeta
                {
                    Name = "test"
                }
            };
            
            var result = KubernetesHelper.CreateNamespace(body);
        }

        public V1NamespaceList ListNamespace()
        {
            return KubernetesHelper.ListNamespace();
        }

        public void DeleteNamespace()
        {
            var status = KubernetesHelper.DeleteNamespace(new V1DeleteOptions(), "test");
        }
        
        
    }
}