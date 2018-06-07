using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace AElf.CLI
{
    class Program
    {
        private static readonly HttpClient Client = new HttpClient();
        private static readonly string RpcServerUrl = "http://localhost:5000/";
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
        }

        private static void ListCommands()
        {
            ushort index = 1;

            foreach (var comm in _commands)
            {
                Console.WriteLine(index + ". " + comm);
                index++;
            }
            
            Console.WriteLine("M. Main Menu\n" + 
                              ("Q. Quit\n"));

            string exec = Console.ReadLine();
            Console.WriteLine();
            
            switch (exec)
            {
                case "M":
                    Menu();
                    break;
                case "Q":
                    Environment.Exit(1);
                    break;
            }
        }

        private static async Task<List<string>> GetCommands()
        {   
//            var json = "jsonrpc?request={\"jsonrpc\":\"2.0\",\"method\":\"get_commands\",\"params\":{ },\"id\":4}";
//            var response = await Client.GetStringAsync(RpcServerUrl + json);
            return null;
        }
    }
}