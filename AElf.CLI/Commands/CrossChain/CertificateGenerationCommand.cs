using System;
using AElf.CLI.Utils;
using CommandLine;

namespace AElf.CLI.Commands.CrossChain
{
    [Verb("gen-cert", HelpText = "Generate certificate for cross chain communication.")]
    public class CertificateGenerationOption : BaseOption
    {
        [Value(0, HelpText = "Chain id.", Required = true)]
        public string ChainId { get; set; }

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
            GenerateCertificate(_option.ChainId, _option.Ip);
        }

        private void GenerateCertificate(string chainId, string ip)
        {
            Pem.WriteCertificate(_option.DataDir, chainId, ip);
            Console.WriteLine(
                $"New generated certificate file with \n" +
                $"ChainId : {chainId} \n" +
                $"IP : {ip} \n" +
                $"Stored in {_option.CertificateDir}");
        }
    }
}