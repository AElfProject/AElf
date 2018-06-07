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
    class Program
    {
        private static List<string> _commands = new List<string>();
        
        public static void Main(string[] args)
        {
            _commands = RpcCalls.GetCommands().Result;
            Menu();
        }

        private static void Menu()
        {
            Console.WriteLine("Welcome to AElf!\n" +
                              "------------------------------------------------\n");
            ushort index = 1;
            
            foreach (var comm in _commands)
            {
                Console.WriteLine(index + ". " + comm);
                index++;
            }
            
            Console.WriteLine("0. Quit\n");

            string exec = Console.ReadLine();
        }
    }
}