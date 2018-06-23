using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Types.CSharp;

namespace AElf.ABI.CSharp
{
    public partial class Method
    {
        private List<Func<string, object>> _parsers;

        private List<Func<string, object>> Parsers
        {
            get
            {
                if (_parsers == null)
                {
                    _parsers = Params.Select(x => StringInputParsers.GetStringParserFor(x.Type)).ToList();
                }

                return _parsers;
            }
        }

        public byte[] SerializeParams(IEnumerable<string> args)
        {
            var argsList = args.ToList();
            if (argsList.Count != Params.Count)
            {
                throw new InvalidInputException("Input doen't have the required number of parameters.");
            }

            var parsed = Parsers.Zip(argsList, Tuple.Create).Select(x => x.Item1(x.Item2)).ToArray();
            return ParamsPacker.Pack(parsed);
        }
    }
}