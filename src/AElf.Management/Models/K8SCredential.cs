using k8s;
using Newtonsoft.Json;

namespace AElf.Management.Models
{
    public class K8SCredential : IKubernetesObject
    {
        [JsonProperty("kind")] public string Kind { get; set; }

        [JsonProperty("apiVersion")] public string ApiVersion { get; set; }

        [JsonProperty("spec")] public K8SCredentialSpec Spec { get; set; }

        [JsonProperty("status")] public K8SCredentialStatus Status { get; set; }
    }

    public class K8SCredentialSpec
    {
    }

    public class K8SCredentialStatus
    {
        [JsonProperty("token")] public string Token { get; set; }
    }
}