using System.Threading.Tasks;
using k8s;

namespace AElf.Management.Helper
{
    public static class K8SRequestHelper
    {
        private static readonly KubernetesClientConfiguration _clientConfiguration = KubernetesClientConfiguration.BuildConfigFromConfigFile();

        public static IKubernetes GetClient()
        {
            _clientConfiguration.AccessToken = AWSTokenHelper.GetToken().Status.Token;
            var client = new Kubernetes(_clientConfiguration);
            return client;
        }

//        public static V1Namespace CreateNamespace(V1Namespace body, string pretty = default(string))
//        {
//            using (var client = GetClient())
//            {
//                return client.CreateNamespace(body, pretty);
//            }
//        }
//
//        public static V1NamespaceList ListNamespace(string continueParameter = default(string), string fieldSelector = default(string), bool? includeUninitialized = default(bool?), string labelSelector = default(string), int? limit = default(int?), string resourceVersion = default(string), int? timeoutSeconds = default(int?), bool? watch = default(bool?), string pretty = default(string))
//        {
//            using (var client = GetClient())
//            {
//                return client.ListNamespace(continueParameter, fieldSelector, includeUninitialized, labelSelector, limit, resourceVersion, timeoutSeconds, watch, pretty);
//            }
//        }
//
//        public static V1Status DeleteNamespace(V1DeleteOptions body, string name, int? gracePeriodSeconds = default(int?), bool? orphanDependents = default(bool?), string propagationPolicy = default(string), string pretty = default(string))
//        {
//            using (var client = GetClient())
//            {
//                return client.DeleteNamespace(body, name, gracePeriodSeconds, orphanDependents, propagationPolicy, pretty);
//            }
//        }
//
//        public static V1Namespace ReadNamespace(string name, bool? exact = null, bool? export = null, string pretty = null)
//        {
//            using (var client = GetClient())
//            {
//                return client.ReadNamespace(name, exact, export, pretty);
//            }
//        }
//
//        public static V1PodList ListNamespacedPod(string namespaceParameter, string continueParameter = null, string fieldSelector = null, bool? includeUninitialized = null, string labelSelector = null, int? limit = null, string resourceVersion = null, int? timeoutSeconds = null, bool? watch = null, string pretty = null)
//        {
//            using (var client = GetClient())
//            {
//                return client.ListNamespacedPod(namespaceParameter, continueParameter, fieldSelector, includeUninitialized, labelSelector, limit, resourceVersion, timeoutSeconds, watch, pretty);
//            }
//        }
//
//        public static Extensionsv1beta1Deployment CreateNamespacedDeployment3(Extensionsv1beta1Deployment body, string namespaceParameter, string pretty = null)
//        {
//            using (var client = GetClient())
//            {
//                return client.CreateNamespacedDeployment3(body, namespaceParameter, pretty);
//            }
//        }
//
//        public static Extensionsv1beta1Deployment PatchNamespacedDeployment3(V1Patch body, string name, string namespaceParameter, string pretty = null)
//        {
//            using (var client = GetClient())
//            {
//                return client.PatchNamespacedDeployment3(body, name, namespaceParameter, pretty);
//            }
//        }
//
//        public static V1Status DeleteNamespacedDeployment3(V1DeleteOptions body, string name, string namespaceParameter, int? gracePeriodSeconds = null, bool? orphanDependents = null, string propagationPolicy = null, string pretty = null)
//        {
//            using (var client = GetClient())
//            {
//                return client.DeleteNamespacedDeployment3(body, name, namespaceParameter, gracePeriodSeconds, orphanDependents, propagationPolicy, pretty);
//            }
//        }
//
//        
//        public static V1ConfigMapList ListNamespacedConfigMap(string namespaceParameter, string continueParameter = null, string fieldSelector = null, bool? includeUninitialized = null, string labelSelector = null, int? limit = null, string resourceVersion = null, int? timeoutSeconds = null, bool? watch = null, string pretty = null)
//        {
//            using (var client = GetClient())
//            {
//                return client.ListNamespacedConfigMap(namespaceParameter, continueParameter, fieldSelector, includeUninitialized, labelSelector, limit, resourceVersion, timeoutSeconds, watch, pretty);
//            }
//        }
//
//        public static V1ConfigMap CreateNamespacedConfigMap(V1ConfigMap body, string namespaceParameter, string pretty = null)
//        {
//            using (var client = GetClient())
//            {
//                return client.CreateNamespacedConfigMap(body, namespaceParameter, pretty);
//            }
//        }
//
//        public static V1Status DeleteNamespacedConfigMap(V1DeleteOptions body, string name, string namespaceParameter, int? gracePeriodSeconds = null, bool? orphanDependents = null, string propagationPolicy = null, string pretty = null)
//        {
//            using (var client = GetClient())
//            {
//                return client.DeleteNamespacedConfigMap(body, name, namespaceParameter, gracePeriodSeconds, orphanDependents, propagationPolicy, pretty);
//            }
//        }
//
//        public static V1ConfigMap PatchNamespacedConfigMap(V1Patch body, string name, string namespaceParameter, string pretty = null)
//        {
//            using (var client = GetClient())
//            {
//                return client.PatchNamespacedConfigMap(body, name, namespaceParameter, pretty);
//            }
//        }
//
//        public static V1beta1StatefulSetList ListNamespacedStatefulSet1(string namespaceParameter, string continueParameter = null, string fieldSelector = null, bool? includeUninitialized = null, string labelSelector = null, int? limit = null, string resourceVersion = null, int? timeoutSeconds = null, bool? watch = null, string pretty = null)
//        {
//            using (var client = GetClient())
//            {
//                return client.ListNamespacedStatefulSet1(namespaceParameter, continueParameter, fieldSelector, includeUninitialized, labelSelector, limit, resourceVersion, timeoutSeconds, watch, pretty);
//            }
//        }
//
//        public static V1beta1StatefulSet CreateNamespacedStatefulSet1(V1beta1StatefulSet body, string namespaceParameter, string pretty = null)
//        {
//            using (var client = GetClient())
//            {
//                return client.CreateNamespacedStatefulSet1(body, namespaceParameter, pretty);
//            }
//        }
//
//        public static V1Status DeleteNamespacedStatefulSet1(V1DeleteOptions body, string name, string namespaceParameter, int? gracePeriodSeconds = null, bool? orphanDependents = null, string propagationPolicy = null, string pretty = null)
//        {
//            using (var client = GetClient())
//            {
//                return client.DeleteNamespacedStatefulSet1(body, name, namespaceParameter, gracePeriodSeconds, orphanDependents, propagationPolicy, pretty);
//            }
//        }
//
//        public static V1beta1StatefulSet PatchNamespacedStatefulSet1(V1Patch body, string name, string namespaceParameter, string pretty = null)
//        {
//            using (var client = GetClient())
//            {
//                return client.PatchNamespacedStatefulSet1(body, name, namespaceParameter, pretty);
//            }
//        }
//
//        public static V1ServiceList ListNamespacedService(string namespaceParameter, string continueParameter = null, string fieldSelector = null, bool? includeUninitialized = null, string labelSelector = null, int? limit = null, string resourceVersion = null, int? timeoutSeconds = null, bool? watch = null, string pretty = null)
//        {
//            using (var client = GetClient())
//            {
//                return client.ListNamespacedService(namespaceParameter, continueParameter, fieldSelector, includeUninitialized, labelSelector, limit, resourceVersion, timeoutSeconds, watch, pretty);
//            }
//        }
//
//        public static V1Service CreateNamespacedService(V1Service body, string namespaceParameter, string pretty = null)
//        {
//            using (var client = GetClient())
//            {
//                return client.CreateNamespacedService(body, namespaceParameter, pretty);
//            }
//        }
//
//        public static V1Status DeleteNamespacedService(V1DeleteOptions body, string name, string namespaceParameter, int? gracePeriodSeconds = null, bool? orphanDependents = null, string propagationPolicy = null, string pretty = null)
//        {
//            using (var client = GetClient())
//            {
//                return client.DeleteNamespacedService(body, name, namespaceParameter, gracePeriodSeconds, orphanDependents, propagationPolicy, pretty);
//            }
//        }
//
//        public static V1Service PatchNamespacedService(V1Patch body, string name, string namespaceParameter, string pretty = null)
//        {
//            using (var client = GetClient())
//            {
//                return client.PatchNamespacedService(body, name, namespaceParameter, pretty);
//            }
//        }
    }
}