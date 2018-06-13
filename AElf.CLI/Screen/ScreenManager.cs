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
        private static string _line;
        private static int _pos;
        private static string _leftSeg;
        private static string _rightSeg;
        
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
            
            while (true)
            {
                if (string.IsNullOrWhiteSpace(command) && Console.CursorLeft <= CliPrefix.Length - 1)
                {
                    Console.Write(CliPrefix);
                }

                _line = CliPrefix + command;
                _pos = Console.CursorLeft;
                
                ConsoleKeyInfo keyInfo = Console.ReadKey();
                switch (keyInfo.Key)
                {
                    case ConsoleKey.UpArrow:
                        if (CommandHistory.Count == 0)
                            continue;
                        
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
                    
                    case ConsoleKey.LeftArrow:
                        if (Console.CursorLeft > CliPrefix.Length)
                        {
                            Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                        }
                        
                        break;
 
                    case ConsoleKey.RightArrow:
                        if (Console.CursorLeft > (CliPrefix.Length + command.Length) - 1)
                            continue;
                        
                        if (Console.CursorLeft < Console.BufferWidth)
                        {
                            Console.SetCursorPosition(Console.CursorLeft + 1, Console.CursorTop);
                        }
                        
                        break;

                    case ConsoleKey.Backspace:
                        if (!string.IsNullOrWhiteSpace(command))
                        {
                            if (Console.CursorLeft > CliPrefix.Length - 1)
                            {
                                Console.SetCursorPosition(Console.CursorLeft - 2, Console.CursorTop);
                                Console.Write(" ");
                                Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                                command = command.Remove(command.Length - 1, 1);
                            }
                            else
                            {
                                Console.SetCursorPosition(CliPrefix.Length, Console.CursorTop);
                            }
                        }
                        else
                        {
                            Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                        }

                        break;
                    
                    case ConsoleKey.Enter:
                        if (string.IsNullOrWhiteSpace(command))
                            continue;
                        
                        CommandHistory.Add(command);
                        return command;
                    
                    default:
                        if (((_line.Length + 1) - Console.CursorLeft) != 0)
                        {
                            _leftSeg = _line.Substring(CliPrefix.Length, _pos - CliPrefix.Length);
                            _rightSeg = _line.Substring((CliPrefix.Length + (_pos - CliPrefix.Length)), _line.Length - (CliPrefix.Length + (_pos - CliPrefix.Length)));
                            
                            _leftSeg += keyInfo.KeyChar;
                            command = _leftSeg + _rightSeg;
                            
                            ClearConsoleLine(command);
                            Console.SetCursorPosition(_pos + 1, Console.CursorTop);
                        }
                        else
                        {
                            command = command + keyInfo.KeyChar;
                        }
                        
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