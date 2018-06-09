using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace AElf.CLI
{
    
    // TODO split the protocols logic from the HTTP/transport logic
    public class RpcCalls
    {
        private static readonly HttpClient Client = new HttpClient();
        private const string RpcServerUrl = "http://localhost:5000";

        public RpcCalls()
        {
            Client.BaseAddress = new Uri(RpcServerUrl);
            Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
        
        public async Task<List<string>> GetCommands()
        {   
            List<string> commands = new List<string>();
            
            var text = "{\"jsonrpc\":\"2.0\",\"method\":\"get_commands\",\"params\":{ },\"id\":0}";
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "/");
            request.Content = new StringContent(text, Encoding.UTF8, "application/json");

            await Client.SendAsync(request)
                .ContinueWith(async responseTask =>
                {
                    string result = await responseTask.Result.Content.ReadAsStringAsync();
                    var j = JObject.Parse(result);
                    var comms = j["result"]["commands"].ToList();

                    commands = comms.Select(c => (string) c).ToList();
                });
            
            return commands;
        }

        public async Task<List<string>> GetPeers(ushort numPeers)
        {
            List<string> peers = new List<string>();

            var text = "{\"jsonrpc\":\"2.0\",\"method\":\"get_peers\",\"params\":{\"numPeers\":\"" + numPeers + "\"},\"id\":1}";
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "/");
            request.Content = new StringContent(text,
                Encoding.UTF8,
                "application/json");

            await Client.SendAsync(request)
                .ContinueWith(async responseTask =>
                {
                    string result = await responseTask.Result.Content.ReadAsStringAsync();
                    var j = JObject.Parse(result);
                    var peersList = j["result"]["data"];

                    foreach (var p in peersList.Children())
                    {
                        peers.Add(p["IpAddress"] + ":" + p["Port"]);
                    }
                });

            return peers;
        }
    }
}