using k8s;
using k8s.Models;

namespace AElf.Deployment.Helper
{
    public static class KubernetesHelper
    {
        private static KubernetesClientConfiguration _clientConfiguration = KubernetesClientConfiguration.BuildConfigFromConfigFile();

        private static IKubernetes GetClient()
        {
            _clientConfiguration.AccessToken = "k8s-aws-v1.aHR0cHM6Ly9zdHMuYW1hem9uYXdzLmNvbS8_QWN0aW9uPUdldENhbGxlcklkZW50aXR5JlZlcnNpb249MjAxMS0wNi0xNSZYLUFtei1BbGdvcml0aG09QVdTNC1ITUFDLVNIQTI1NiZYLUFtei1DcmVkZW50aWFsPUFLSUFKWlEzTkJTTVlWVE1ORk5RJTJGMjAxODA4MDIlMkZ1cy1lYXN0LTElMkZzdHMlMkZhd3M0X3JlcXVlc3QmWC1BbXotRGF0ZT0yMDE4MDgwMlQxMTUwMzVaJlgtQW16LUV4cGlyZXM9NjAmWC1BbXotU2lnbmVkSGVhZGVycz1ob3N0JTNCeC1rOHMtYXdzLWlkJlgtQW16LVNpZ25hdHVyZT05ZWFjODc4ZDM1NzBlNTNiOTAxZTRlNGE2MGVkMzhkYzY3ZmI2M2EwNDY3MzFlYTUyNzk3OTJmZTE0ZjA4MTBh";
            var client = new Kubernetes(_clientConfiguration);
            return client;
        }

        public static V1PodList ListNamespacedPod(string namespaceParameter, string continueParameter = null, string fieldSelector = null, bool? includeUninitialized = null, string labelSelector = null, int? limit = null, string resourceVersion = null, int? timeoutSeconds = null, bool? watch = null, string pretty = null)
        {
            using (var client = GetClient())
            {
                return client.ListNamespacedPod(namespaceParameter, continueParameter, fieldSelector, includeUninitialized, labelSelector, limit, resourceVersion, timeoutSeconds, watch, pretty);
            }
        }

        public static Extensionsv1beta1Deployment CreateNamespacedDeployment3(Extensionsv1beta1Deployment body, string namespaceParameter, string pretty = null)
        {
            using (var client = GetClient())
            {
                return client.CreateNamespacedDeployment3(body, namespaceParameter, pretty);
            }
        }

        public static Extensionsv1beta1Deployment ReplaceNamespacedDeployment3(Extensionsv1beta1Deployment body, string name, string namespaceParameter, string pretty = null)
        {
            using (var client = GetClient())
            {
                return client.ReplaceNamespacedDeployment3(body, name, namespaceParameter, pretty);
            }
        }

        public static Extensionsv1beta1Deployment PatchNamespacedDeployment3(V1Patch body, string name, string namespaceParameter, string pretty = null)
        {
            using (var client = GetClient())
            {
                return client.PatchNamespacedDeployment3(body, name, namespaceParameter, pretty);
            }
        }

        public static V1Status DeleteNamespacedDeployment3(V1DeleteOptions body, string name, string namespaceParameter, int? gracePeriodSeconds = null, bool? orphanDependents = null, string propagationPolicy = null, string pretty = null)
        {
            using (var client = GetClient())
            {
                return client.DeleteNamespacedDeployment3(body, name, namespaceParameter, gracePeriodSeconds, orphanDependents, propagationPolicy, pretty);
            }
        }

        public static V1Namespace CreateNamespace(V1Namespace body, string pretty = default(string))
        {
            using (var client = GetClient())
            {
                return client.CreateNamespace(body, pretty);
            }
        }

        public static V1NamespaceList ListNamespace(string continueParameter = default(string), string fieldSelector = default(string), bool? includeUninitialized = default(bool?), string labelSelector = default(string), int? limit = default(int?), string resourceVersion = default(string), int? timeoutSeconds = default(int?), bool? watch = default(bool?), string pretty = default(string))
        {
            using (var client = GetClient())
            {
                return client.ListNamespace(continueParameter, fieldSelector, includeUninitialized, labelSelector, limit, resourceVersion, timeoutSeconds, watch, pretty);
            }
        }

        public static V1Status DeleteNamespace(V1DeleteOptions body, string name, int? gracePeriodSeconds = default(int?), bool? orphanDependents = default(bool?), string propagationPolicy = default(string), string pretty = default(string))
        {
            using (var client = GetClient())
            {
                return client.DeleteNamespace(body, name, gracePeriodSeconds, orphanDependents, propagationPolicy, pretty);
            }
        }

        public static V1ConfigMapList ListNamespacedConfigMap(string namespaceParameter, string continueParameter = null, string fieldSelector = null, bool? includeUninitialized = null, string labelSelector = null, int? limit = null, string resourceVersion = null, int? timeoutSeconds = null, bool? watch = null, string pretty = null)
        {
            using (var client = GetClient())
            {
                return client.ListNamespacedConfigMap(namespaceParameter, continueParameter, fieldSelector, includeUninitialized, labelSelector, limit, resourceVersion, timeoutSeconds, watch, pretty);
            }
        }

        public static V1ConfigMap CreateNamespacedConfigMap(V1ConfigMap body, string namespaceParameter, string pretty = null)
        {
            using (var client = GetClient())
            {
                return client.CreateNamespacedConfigMap(body, namespaceParameter, pretty);
            }
        }

        public static V1Status DeleteNamespacedConfigMap(V1DeleteOptions body, string name, string namespaceParameter, int? gracePeriodSeconds = null, bool? orphanDependents = null, string propagationPolicy = null, string pretty = null)
        {
            using (var client = GetClient())
            {
                return client.DeleteNamespacedConfigMap(body, name, namespaceParameter, gracePeriodSeconds, orphanDependents, propagationPolicy, pretty);
            }
        }

        public static V1ConfigMap PatchNamespacedConfigMap(V1Patch body, string name, string namespaceParameter, string pretty = null)
        {
            using (var client = GetClient())
            {
                return client.PatchNamespacedConfigMap(body, name, namespaceParameter, pretty);
            }
        }



    }
}