using System;
using System.Collections.Generic;
using Autofac;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking;

namespace AElf.CLI2.Commands
{
    public class CMDModule : Module
    {
        private readonly BaseOption _option;

        private static readonly IDictionary<Type, Type> _commands;

        public CMDModule(BaseOption option)
        {
            _option = option;
        }

        static CMDModule()
        {
            _commands = new Dictionary<Type, Type> {[typeof(AccountNewOption)] = typeof(AccountNewCommand)};
        }

        protected override void Load(ContainerBuilder builder)
        {
            _option.ParseEnvVars();
            var cmdType = _commands[_option.GetType()];
            builder.RegisterInstance(_option);
            builder.RegisterType(cmdType).As<ICommand>();
            base.Load(builder);
        }
    }
}