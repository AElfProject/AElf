using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security;

namespace AElf.CLI.Screen
{
    public class ScreenManager
    {
        private const string CliPrefix = "> AElf$ ";
        private const string Usage = "Usage: command_name <param1> <param2> ...";
        private const string Quit = "To Quit: Type 'Quit'";
        
        private static readonly List<string> CommandHistory = new List<string>();
        private static int _chIndex = 0;
        
        public void PrintHeader()
        {
            PrintSeparator();
            Console.WriteLine("Welcome to AElf!");
            PrintSeparator();
        }

        public void PrintSeparator()
        {
            Console.WriteLine("------------------------------------------------");
        }

        public void PrintUsage()
        {
            Console.WriteLine("Use \"getcommands\" to print available commands");
            Console.WriteLine(Usage);
            Console.WriteLine(Quit);
        }

        public string GetCommand()
        {
            string command = "";
            
            Console.Write(CliPrefix);
            
            while (true)
            {
                ConsoleKeyInfo keyInfo = Console.ReadKey();
                switch (keyInfo.Key)
                {
                    case ConsoleKey.UpArrow:
                        if (_chIndex == 0)
                        {
                            _chIndex = CommandHistory.Count;
                            command = CommandHistory.ElementAt(_chIndex - 1);
                            ClearConsoleLine(command);
                            _chIndex--;
                        }
                        else
                        {
                            if (_chIndex == 0)
                            {
                                _chIndex = 0;
                                ClearConsoleLine("");
                                command = "";
                            }
                            else
                            {
                                command = CommandHistory.ElementAt(_chIndex - 1);
                                ClearConsoleLine(command);
                                _chIndex--;
                            }
                        }

                        break;
                    
                    case ConsoleKey.DownArrow:
                        if (CommandHistory.Count > _chIndex)
                        {
                            command = CommandHistory.ElementAt(_chIndex);
                            ClearConsoleLine(command);
                            _chIndex++;
                        }
                        else
                        {
                            _chIndex = 0;
                            ClearConsoleLine("");
                            command = "";
                        }

                        break;

                    case ConsoleKey.Backspace:
                        if (Console.CursorLeft > CliPrefix.Length - 1)
                        {
                            Console.Write(new string(' ', 1));
                            Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                            command = command.Remove(command.Length - 1, 1);
                        }
                        else
                        {
                            Console.SetCursorPosition(CliPrefix.Length, Console.CursorTop);
                        }

                        break;
                    
                    case ConsoleKey.Enter:
                        if (!string.IsNullOrWhiteSpace(command))
                            CommandHistory.Add(command);
                        return command;
                    
                    default:
                        command = command + keyInfo.KeyChar;
                        break;
                }
            }
        }
        
        static void ClearConsoleLine(string command)
        {
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new String(' ', Console.BufferWidth - CliPrefix.Length));
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(CliPrefix + command);
        }

        public void PrintCommandNotFount(string command)
        {
            Console.WriteLine(command + " : command not found");
        }

        public void PrintError(string error)
        {
            Console.WriteLine(error);
        }

        public void PrintLine(string str)
        {
            Console.WriteLine(str);
        }

        public void PrintLine()
        {
            PrintLine("\n");
        }

        public void Print(string str)
        {
            Console.Write(str);
        }

        public string AskInvisible(string prefix)
        {
            Print(prefix);
            
            var pwd = new SecureString();
            while (true)
            {
                ConsoleKeyInfo i = Console.ReadKey(true);
                if (i.Key == ConsoleKey.Enter)
                {
                    break;
                }
                else if (i.Key == ConsoleKey.Backspace)
                {
                    if (pwd.Length > 0)
                    {
                        pwd.RemoveAt(pwd.Length - 1);
                    }
                }
                else
                {
                    pwd.AppendChar(i.KeyChar);
                    //Console.Write("*");
                }
            }
            
            Console.WriteLine();
            
            return new NetworkCredential("", pwd).Password;
        }
    }
}