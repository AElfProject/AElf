using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
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

        public int Category { get; } = 0;

        public IOptionsMonitor<CSharpCodeOpsOptions> CodeOpsOptionsMonitor { get; set; }

        private readonly IPolicy _policy;

        public CSharpContractAuditor(IPolicy policy, IAcsValidator acsValidator)
        {
            _policy = policy;
            _acsValidator = acsValidator;
        }

        public void Audit(byte[] code, RequiredAcs requiredAcs, bool isSystemContract)
        {
            var findings = new List<ValidationResult>();
            var asm = Assembly.Load(code);
            var modDef = ModuleDefinition.ReadModule(new MemoryStream(code));
            var cts = new CancellationTokenSource(CodeOpsOptionsMonitor?.CurrentValue.AuditTimeoutDuration ??
                                                  Constants.DefaultAuditTimeoutDuration);
            // Run module validators
            findings.AddRange(Validate(modDef, cts.Token, isSystemContract));

            // Run assembly validators (run after module validators since we invoke BindService method below)
            findings.AddRange(Validate(asm, cts.Token, isSystemContract));

            // Run method validators
            foreach (var type in modDef.Types)
            {
                findings.AddRange(ValidateMethodsInType(type, cts.Token, isSystemContract));
            }

            // Perform ACS validation
            if (requiredAcs != null)
                findings.AddRange(_acsValidator.Validate(asm, requiredAcs));

            if (findings.Count > 0)
            {
                throw new CSharpCodeCheckException(
                    $"Contract code did not pass audit. Audit failed for contract: {modDef.Assembly.MainModule.Name}\n" +
                    string.Join("\n", findings), findings);
            }
        }

        private IEnumerable<ValidationResult> Validate<T>(T t, CancellationToken ct, bool isSystemContract)
        {
            var validators = _policy.GetValidators<T>().Where(v => !v.SystemContactIgnored || !isSystemContract);
            return validators.SelectMany(v => v.Validate(t, ct));
        }
        
        private IEnumerable<ValidationResult> ValidateMethodsInType(TypeDefinition type,
            CancellationToken ct, bool isSystemContract)
        {
            var findings = new List<ValidationResult>();

            foreach (var method in type.Methods)
            {
                findings.AddRange(Validate(method, ct, isSystemContract));
            }

            foreach (var nestedType in type.NestedTypes)
            {
                findings.AddRange(ValidateMethodsInType(nestedType, ct, isSystemContract));
            }

            return findings;
        }
    }
}