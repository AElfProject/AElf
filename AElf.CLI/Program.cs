using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
            Menu();
        }

        private static async Task Menu()
        {
            Console.WriteLine("Welcome to AElf!\n" +
                              "------------------------------------------------\n");
            ListCommands();
            
            var text = "{\"jsonrpc\":\"2.0\",\"method\":\"get_commands\",\"params\":{ },\"id\":0}";
            var j = JObject.Parse(text);
            var content = new StringContent(j.ToString());
            
            Client.BaseAddress = new Uri(RpcServerUrl);
            Client.DefaultRequestHeaders.Accept.Clear();
            Client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            HttpResponseMessage response = await Client.PostAsync(RpcServerUrl, content);
            
            Console.WriteLine(response.Content.ToString());
        }

        private static void ListCommands()
        {
            ushort index = 0;

            Console.WriteLine(index + ". Return to Main Menu");
            index++;
            
            foreach (var comm in _commands)
            {
                Console.WriteLine(index + ". " + comm);
                index++;
            }
            
            Console.WriteLine("Q. Quit\n");

            string exec = Console.ReadLine();
            Console.WriteLine();
            
            switch (exec)
            {
                case "0":
                    Menu();
                    break;
                case "Q":
                    Environment.Exit(1);
                    break;
            }
        }

        private static async Task<List<string>> GetCommands()
        {   
//            var content = "jsonrpc?request={\"jsonrpc\":\"2.0\",\"method\":\"get_commands\",\"params\":{ },\"id\":4}";
//            var response = await Client.GetStringAsync(RpcServerUrl + content);
            return null;
        }
    }
}