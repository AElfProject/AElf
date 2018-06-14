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
        private static string _currentLine;
        private static int _lineCursorPosition;
        private static int _commandCursorPosition;
        private static string _leftSegCommand;
        private static string _rightSegCommand;
        
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

                _currentLine = CliPrefix + command;
                _lineCursorPosition = Console.CursorLeft;
                _commandCursorPosition = _lineCursorPosition - CliPrefix.Length;
                
                ConsoleKeyInfo keyInfo = Console.ReadKey();
                switch (keyInfo.Key)
                {
                    case ConsoleKey.UpArrow:
                        if (CommandHistory.Count == 0)
                            continue;
                        
                        command = UpArrowKey(command);
                        break;
                    
                    case ConsoleKey.DownArrow:
                        command = DownArrowKey(command);
                        break;
                    
                    case ConsoleKey.LeftArrow:
                        LeftArrowKey();
                        break;
 
                    case ConsoleKey.RightArrow:
                        if (Console.CursorLeft > (CliPrefix.Length + command.Length) - 1)
                            continue;
                        
                        RightArrowKey();
                        break;

                    case ConsoleKey.Backspace:
                        if (command.Length == 0 || _commandCursorPosition < 1)
                        {
                            ClearConsoleLine(command);
                            Console.SetCursorPosition(_lineCursorPosition, Console.CursorTop);
                            continue;
                        }

                        command = BackspaceKey(command);
                        break;
                    
                    case ConsoleKey.Enter:
                        if (string.IsNullOrWhiteSpace(command))
                            continue;
                        
                        CommandHistory.Add(command);
                        return command;
                    
                    default:
                        command = DefaultKey(command, keyInfo.KeyChar);
                        break;
                }
            }
        }

        static string UpArrowKey(string command)
        {     
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

            return command;
        }

        static string DownArrowKey(string command)
        {
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

            return command;
        }

        static void LeftArrowKey()
        {
            if (Console.CursorLeft > CliPrefix.Length)
            {
                Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
            }
        }

        static void RightArrowKey()
        {
            if (Console.CursorLeft < Console.BufferWidth)
            {
                Console.SetCursorPosition(Console.CursorLeft + 1, Console.CursorTop);
            }
        }

        static string BackspaceKey(string command)
        {
            if ((_currentLine.Length + 1) - Console.CursorLeft != 0)
            {
                _leftSegCommand = _currentLine.Substring(CliPrefix.Length, _commandCursorPosition);
                            
                _rightSegCommand = _currentLine.Substring(
                    (CliPrefix.Length + _commandCursorPosition),
                    _currentLine.Length - (CliPrefix.Length + _commandCursorPosition));

                _leftSegCommand = _leftSegCommand.Remove(_leftSegCommand.Length - 1);
                command = _leftSegCommand + _rightSegCommand;

                ClearConsoleLine(command);
                Console.SetCursorPosition(_lineCursorPosition - 1, Console.CursorTop);
            }
            else
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

            return command;
        }

        static string DefaultKey(string command, char keyChar)
        {
            if (((_currentLine.Length + 1) - Console.CursorLeft) != 0)
            {
                _leftSegCommand = _currentLine.Substring(CliPrefix.Length, _commandCursorPosition);
                            
                _rightSegCommand = _currentLine.Substring(
                    (CliPrefix.Length + _commandCursorPosition),
                    _currentLine.Length - (CliPrefix.Length + _commandCursorPosition));
                            
                _leftSegCommand += keyChar;
                command = _leftSegCommand + _rightSegCommand;
                            
                ClearConsoleLine(command);
                Console.SetCursorPosition(_lineCursorPosition + 1, Console.CursorTop);
            }
            else
            {
                command = command + keyChar;
            }

            return command;
        }
        
        static void ClearConsoleLine(string command)
        {
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new String(' ', Console.BufferWidth - CliPrefix.Length));
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(CliPrefix + command);
        }

        public void PrintCommandNotFound(string command)
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