using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace AElf.CLI.Command
{
    public class GetPeersCommand : ICommand
    {
        public string Process(IEnumerable<string> args, AElfClientProgramContext context)
        {
            if (args.Count() >= 2)
            {
                throw new InvalidArgumentNumberException();
            }
            if (!int.TryParse(args.Any() ? args.First() : "-1", out var limit))
            {
                throw new CommandException("<limit> in get_peers must be integer");
            }

            return context.RPCClient.Request("get_peers", limit <= 0
                ? new JObject()
                {
                    ["numPeers"] = null
                }
                : new JObject()
                {
                    ["numPeers"] = limit
                });
        }

        public string Usage { get; } = @"get_peers [<limit>]
    where <limit> is the max number of peers to return.";
    }
}