using System;
using System.Collections.Generic;
using Autofac;

namespace AElf.CLI2.Commands
{
    public class CmdModule : Module
    {
        private readonly BaseOption _option;

        private static readonly IDictionary<Type, Type> _commands;

        public CmdModule(BaseOption option)
        {
            _option = option;
        }

        static CmdModule()
        {
            _commands = new Dictionary<Type, Type>
            {
                [typeof(CreateOption)] = typeof(CreateCommand),
                [typeof(InteractiveOption)] = typeof(InteractiveCommand),
                [typeof(DeployOption)] = typeof(DeployCommand),
                [typeof(GetAbiOption)] = typeof(GetAbiCommand),
                [typeof(SendTransactionOption)] = typeof(SendTransactionCommand),
                [typeof(GetTxResultOption)] = typeof(GetTxResultCommand),
                [typeof(GetBlockHeightOption)] = typeof(GetBlockHeightCommand),
                [typeof(GetBlockInfoOption)] = typeof(GetBlockInfoCommand),
            };
        }

        protected override void Load(ContainerBuilder builder)
        {
//            _option.ParseEnvVars();
            var cmdType = _commands[_option.GetType()];
            builder.RegisterInstance(_option);
            builder.RegisterType(cmdType).As<Command>();
            base.Load(builder);
        }
    }
}