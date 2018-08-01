using k8s;
using k8s.Models;

namespace AElf.Deployment.Helper
{
    public static class KubernetesHelper
    {
        private static KubernetesClientConfiguration _clientConfiguration = KubernetesClientConfiguration.BuildConfigFromConfigFile();

        private static IKubernetes GetClient()
        {
            _clientConfiguration.AccessToken = "k8s-aws-v1.aHR0cHM6Ly9zdHMuYW1hem9uYXdzLmNvbS8_QWN0aW9uPUdldENhbGxlcklkZW50aXR5JlZlcnNpb249MjAxMS0wNi0xNSZYLUFtei1BbGdvcml0aG09QVdTNC1ITUFDLVNIQTI1NiZYLUFtei1DcmVkZW50aWFsPUFLSUFKWlEzTkJTTVlWVE1ORk5RJTJGMjAxODA4MDElMkZ1cy1lYXN0LTElMkZzdHMlMkZhd3M0X3JlcXVlc3QmWC1BbXotRGF0ZT0yMDE4MDgwMVQxMTIxMzVaJlgtQW16LUV4cGlyZXM9NjAmWC1BbXotU2lnbmVkSGVhZGVycz1ob3N0JTNCeC1rOHMtYXdzLWlkJlgtQW16LVNpZ25hdHVyZT0yYjZiZTBjN2U4YzBmNjU1YWIxZjc4MmIxZmM3YjFiYzcxYjI0NDYwNjAxNDMxYjliZjczZWIxM2QzZDMwMWI3";
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

        public static V1Scale ReplaceNamespacedDeploymentScale(V1Scale body, string name, string namespaceParameter, string pretty = default(string))
        {
            using (var client = GetClient())
            {
                return client.ReplaceNamespacedDeploymentScale(body, name, namespaceParameter, pretty);
            }
        }
    }
}