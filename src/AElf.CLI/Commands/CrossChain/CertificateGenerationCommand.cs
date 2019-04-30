using System;
using AElf.CLI.Utils;
using CommandLine;

namespace AElf.CLI.Commands.CrossChain
{
    [Verb("gen-cert", HelpText = "Generate certificate for cross chain communication.")]
    public class CertificateGenerationOption : BaseOption
    {
        [Value(0, HelpText = "Name.", Required = true)]
        public string Name { get; set; }

        [Value(1, HelpText = "Ip address for this node.", Required = true)]
        public string Ip { get; set; }
    }

    public class CertificateGenerationCommand : Command
    {
        private readonly CertificateGenerationOption _option;

        public CertificateGenerationCommand(CertificateGenerationOption option) : base(option)
        {
            _option = option;
        }

        public override void Execute()
        {
            GenerateCertificate(_option.Name, _option.Ip);
        }

        private void GenerateCertificate(string name, string ip)
        {
            Pem.WriteCertificate(_option.DataDir, name, ip);
            Console.WriteLine(
                $"New generated certificate file with \n" +
                $"Name : {name} \n" +
                $"IP : {ip} \n" +
                $"Stored in {_option.CertificateDir}");
        }
    }
}