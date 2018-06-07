using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.Design.Serialization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AElf.CLI
{
    class Program
    {
        private static readonly HttpClient Client = new HttpClient();
        private static readonly string RpcServerUrl = "http://localhost:5000";
        private static List<string> _commands = new List<string>();
        
        public static void Main(string[] args)
        {
            _commands = GetCommands().Result;
            Menu();
        }

        private static void Menu()
        {
            Console.WriteLine("Welcome to AElf!\n" +
                              "------------------------------------------------\n");
            ushort index = 0;

            Console.WriteLine(index + ". Return to Main Menu");
            index++;
            
            foreach (var comm in _commands)
            {
                Console.WriteLine(index + ". " + comm);
                index++;
            }
            
            Console.WriteLine("Q. Quit\n");

            Console.ReadLine();
        }

        private static async Task<List<string>> GetCommands()
        {   
            List<string> commands = new List<string>();
            
            var text = "{\"jsonrpc\":\"2.0\",\"method\":\"get_commands\",\"params\":{ },\"id\":0}";
            Client.BaseAddress = new Uri(RpcServerUrl);
            Client.DefaultRequestHeaders
                .Accept
                .Add(new MediaTypeWithQualityHeaderValue("application/json"));
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "/");
            request.Content = new StringContent(text,
                Encoding.UTF8,
                "application/json");

            await Client.SendAsync(request)
                .ContinueWith(async responseTask =>
                {
                    var response = responseTask.Result;
                    var jsonString = response.Content.ReadAsStringAsync();
                    string result = await jsonString;
                    var j = JObject.Parse(result);
                    var comms = j["result"]["commands"].ToList();

                    foreach (var c in comms)
                    {
                        commands.Add(c.ToString());
                    }
                });
            
            return commands;
        }
    }
}