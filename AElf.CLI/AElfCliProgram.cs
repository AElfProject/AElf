using System;
using AElf.CLI.Screen;

namespace AElf.CLI
{
    public class AElfCliProgram
    {
        private const string ExitReplCommand = "quit";
        
        private ScreenManager _screenManager = new ScreenManager();
        
        public AElfCliProgram(ScreenManager _screenManager)
        {
            _screenManager = _screenManager;
        }

        public void StartRepl()
        {
            _screenManager.PrintHeader();
            _screenManager.PrintUsage();
            
            while (true)
            {
                string command = _screenManager.GetCommand();

                // stop the repla
                if (command.Equals(ExitReplCommand, StringComparison.OrdinalIgnoreCase))
                {
                    Stop();
                    break;
                }


            }
        }

        private void Stop()
        {
            
        }
    }
}