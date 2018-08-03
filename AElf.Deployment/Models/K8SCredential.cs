using Newtonsoft.Json;

namespace AElf.Deployment.Models
{
    public class K8SCredential
    {
        [JsonProperty("kind")]
        public string kind { get; set; }

        [JsonProperty("apiVersion")]
        public string ApiVersion { get; set; }

        [JsonProperty("spec")]
        public K8SCredentialSpec Spec { get; set; }

        [JsonProperty("status")]
        public K8SCredentialStatus Status { get; set; }

    }

    public class K8SCredentialSpec
    {
    }

    public class K8SCredentialStatus
    {
        [JsonProperty("token")]
        public string Token { get; set; }
    }
}