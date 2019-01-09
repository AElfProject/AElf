using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Types.CSharp;
using Method = AElf.Kernel.ABI.Method;

namespace AElf.Runtime.CSharp.Core.ABI
{
    public static class MethodExtensions
    {
        public static byte[] SerializeParams(this Method method, IEnumerable<string> args)
        {
            var argsList = args.ToList();
            if (argsList.Count != method.Params.Count)
            {
                throw new InvalidInputException("Input doen't have the required number of parameters.");
            }

            var parsers = method.Params.Select(x => StringInputParsers.GetStringParserFor(x.Type));
            var parsed = parsers.Zip(argsList, Tuple.Create).Select(x => x.Item1(x.Item2)).ToArray();
            return ParamsPacker.Pack(parsed);
        }

        public static IEnumerable<string> DeserializeParams(this Method method, IEnumerable<object> args,
            IEnumerable<Type> types = null)
        {
            var argsList = args.ToList();
            if (argsList.Count != method.Params.Count)
            {
                throw new InvalidInputException("Input doen't have the required number of parameters.");
            }

            var deserializers = method.Params.Select(x => StringInputParsers.ParseToStringFor(x.Type, types));
            var parsed = deserializers.Zip(argsList, Tuple.Create).Select(x => x.Item1(x.Item2)).ToList();
            return parsed;
        }
    }
}