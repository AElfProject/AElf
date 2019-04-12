using AElf.Management.Models;
using Newtonsoft.Json;

namespace AElf.Management.Helper
{
    public class AWSTokenHelper
    {
        public static K8SCredential GetToken()
        {
            var jsonResult = GetResultFromTerminal("heptio-authenticator-aws token -i aelf-blockchain-test-net");
            var credential = JsonConvert.DeserializeObject<K8SCredential>(jsonResult);

            return credential;
        }

        private static string GetResultFromTerminal(string cmd)
        {
            var p = new System.Diagnostics.Process
            {
                StartInfo =
                {
                    FileName = "bash",
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            p.Start();
            p.StandardInput.WriteLine(cmd);
            p.StandardInput.WriteLine("exit");
            var result = p.StandardOutput.ReadToEnd();
            p.Close();

            return result;
        }
    }
}