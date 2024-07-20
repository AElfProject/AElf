using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AElf.CSharp.CodeOps.Policies;
using AElf.CSharp.CodeOps.Validators;
using AElf.CSharp.CodeOps.Validators.Assembly;
using AElf.Kernel.CodeCheck.Infrastructure;
using AElf.Kernel.SmartContract;
using Microsoft.Extensions.Options;
using Mono.Cecil;

namespace AElf.CSharp.CodeOps
{
    public class CSharpContractAuditor : IContractAuditor
    {
        private readonly IAcsValidator _acsValidator;
        private readonly IPolicy _policy;

        public int Category { get; } = 0;
        public IOptionsMonitor<CSharpCodeOpsOptions> CodeOpsOptionsMonitor { get; set; }

        public CSharpContractAuditor(IPolicy policy, IAcsValidator acsValidator)
        {
            _policy = policy;
            _acsValidator = acsValidator;
        }

        public void Audit(byte[] code, RequiredAcs requiredAcs, bool isSystemContract)
        {
            var findings = new ConcurrentBag<ValidationResult>();
            var asm = Assembly.Load(code);
            var modDef = ModuleDefinition.ReadModule(new MemoryStream(code));
            var cts = new CancellationTokenSource(CodeOpsOptionsMonitor?.CurrentValue.AuditTimeoutDuration ??
                                                  Constants.DefaultAuditTimeoutDuration);

            // Run module validators
            var moduleFindings = Validate(modDef, cts.Token, isSystemContract);
            foreach (var finding in moduleFindings)
            {
                findings.Add(finding);
            }

            // Run assembly validators
            var assemblyFindings = Validate(asm, cts.Token, isSystemContract);
            foreach (var finding in assemblyFindings)
            {
                findings.Add(finding);
            }

            // Run method validators
            foreach (var type in modDef.Types)
            {
                var methodFindings = ValidateMethodsInType(type, cts.Token, isSystemContract);
                foreach (var finding in methodFindings)
                {
                    findings.Add(finding);
                }
            }


            // Perform ACS validation
            if (requiredAcs != null)
            {
                var acsFindings = _acsValidator.Validate(asm, requiredAcs);
                foreach (var finding in acsFindings)
                {
                    findings.Add(finding);
                }
            }

            if (findings.Count > 0)
            {
                throw new CSharpCodeCheckException(
                    $"Contract code did not pass audit. Audit failed for contract: {modDef.Assembly.MainModule.Name}\n" +
                    string.Join("\n", findings), findings.ToList());
            }
        }

        private IEnumerable<ValidationResult> Validate<T>(T t, CancellationToken ct, bool isSystemContract)
        {
            var validators = _policy.GetValidators<T>().AsParallel()
                .Where(v => !v.SystemContactIgnored || !isSystemContract);

            var results = new ConcurrentBag<ValidationResult>();
            foreach (var v in validators)
            {
                foreach (var result in v.Validate(t, ct))
                {
                    results.Add(result);
                }
            }

            return results;
        }

        private IEnumerable<ValidationResult> ValidateMethodsInType(TypeDefinition type,
            CancellationToken ct, bool isSystemContract)
        {
            var findings = new ConcurrentBag<ValidationResult>();

            Parallel.ForEach(type.Methods, method =>
            {
                foreach (var finding in Validate(method, ct, isSystemContract))
                {
                    findings.Add(finding);
                }
            });

            Parallel.ForEach(type.NestedTypes, nestedType =>
            {
                foreach (var finding in ValidateMethodsInType(nestedType, ct, isSystemContract))
                {
                    findings.Add(finding);
                }
            });

            return findings;
        }
    }
}
