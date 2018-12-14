using Alba.CsConsoleFormat.Fluent;
using ChakraCore.NET.API;
using CommandLine;

namespace AElf.CLI2.Commands
{
    [Verb("send", HelpText = "Send a transaction to a contract.")]
    public class SendTransactionOption : BaseOption
    {
        [Value(0, HelpText = "The address of the contract.", Required = true)]
        public string Contract { get; set; } = "";

        [Value(1, HelpText = "The particular method of the contract.", Required = true)]
        public string Method { get; set; } = "";

        [Value(2, HelpText = "The parameters for the method in json array format.", Required = true)]
        public string Params { get; set; } = "";
    }

    public class SendTransactionCommand : Command
    {
        private readonly SendTransactionOption _option;

        public SendTransactionCommand(SendTransactionOption option) : base(option)
        {
            _option = option;
        }

        public override void Execute()
        {
            if (string.IsNullOrEmpty(_option.Endpoint))
            {
                Colors.WriteLine("Endpoint is not provided. Cannot proceed.".DarkRed());
                return;
            }

            if (string.IsNullOrEmpty(_option.Account))
            {
                Colors.WriteLine("Account is not provided. Cannot proceed.".DarkRed());
                return;
            }

            InitChain();
            try
            {
                // Get contract and method
                _engine.RunScript($@"
                    var contract = aelf.chain.contractAt('{_option.Contract}', _account);
                    var method = contract['{_option.Method}'];
                ");

                // Prepare arguments
                _engine.RunScript($@"
                    var methodargs = JSON.parse('{_option.Params}');
                ");
                _engine.RunScript($@"
                    var methodAbi = contract.abi.Methods.find(x => x.Name === '{_option.Method}');
                ");
                var inputCount = _engine.Evaluate("methodargs").ReadProperty<int>("length");
                var reqCount = _engine.Evaluate("methodAbi.Params").ReadProperty<int>("length");
                if (inputCount != reqCount)
                {
                    Colors.WriteLine(
                        $@"Method ""{_option.Method}"" on contract ""{_option.Contract}"" requires {reqCount} input arguments."
                            .DarkRed());
                    return;
                }

                // Execute
                _engine.Execute(@"method.apply(null, methodargs);");
            }
            catch (JavaScriptException e)
            {
                Colors.WriteLine(e.Message.Replace("Script threw an exception. ", "").DarkRed());
            }
        }
    }
}