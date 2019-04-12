using Alba.CsConsoleFormat.Fluent;
using ChakraCore.NET.API;
using CommandLine;

namespace AElf.CLI.Commands
{
    public class SendOrCallOption : BaseOption
    {
        [Value(0, HelpText = "The address of the contract.", Required = true)]
        public string Contract { get; set; } = "";

        [Value(1, HelpText = "The particular method of the contract.", Default = "")]
        public string Method { get; set; } = "";

        [Value(2, HelpText = "The input for the method in json format.", Default = "")]
        public string Input { get; set; } = "";
    }

    public class SendOrCallBase : Command
    {
        private readonly SendOrCallOption _option;
        protected bool _isCall = false;

        protected SendOrCallBase(SendOrCallOption option) : base(option)
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

            // TODO: Skip loading _account for call
            InitChain();
            try
            {
                // Get contract
                _engine.RunScript($@"
                    var contract = aelf.chain.contractAt('{_option.Contract}', _account);
                    if(!contract)
                        throw new Error('Failed to initialize contract at {_option.Contract}. Make sure the address is a valid one.');
                ");

                // No method specified
                if (string.IsNullOrWhiteSpace(_option.Method))
                {
                    _engine.Execute($@"
                        if(contract){{
                            var names = contract.service.methodsArray.map(x=>x.name).join('\n');
                            'Method name is required for sending a transaction:\n' + names + '\n';
                        }}
                    ");
                    return;
                }

                _engine.RunScript($@"
                    var method = contract['{_option.Method}'];
                    if(!method){{
                        var names = contract.service.methodsArray.map(x=>x.name).join('\n');
                        throw new Error('Method {_option.Method} is not found in the contract at {_option.Contract}.\n'
                            + 'Valid method names are:\n'+names);
                    }}
                ");

                // No input is given
                if (string.IsNullOrWhiteSpace(_option.Input))
                {
                    _engine.Execute($@"
                        if(method)
                            method.inputTypeInfo;
                    ");
                    return;
                }

                // Check param fields
                _engine.RunScript($@"
                    var input = {_option.Input};
                    var keys = Object.keys(input);
                    for(var i = 0; i < keys.length; i++){{
                        if(!method.inputTypeInfo.fields.hasOwnProperty(keys[i])){{
                            throw new Error('Invalid field name: '+keys[i]);
                        }}
                    }}
                ");

                // Execute
                _engine.Execute(_isCall ? "method.call(input);" : "method(input);");
            }
            catch (JavaScriptException e)
            {
                Colors.WriteLine(e.Message.Replace("Script threw an exception. ", "").DarkRed());
            }
        }
    }
}