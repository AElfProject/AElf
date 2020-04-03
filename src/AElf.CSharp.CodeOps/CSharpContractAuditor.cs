using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using AElf.CSharp.CodeOps.Policies;
using AElf.CSharp.CodeOps.Validators;
using AElf.CSharp.CodeOps.Validators.Assembly;
using AElf.Kernel.CodeCheck.Infrastructure;
using AElf.Kernel.SmartContract.Application;
using Microsoft.Extensions.Options;
using Mono.Cecil;


namespace AElf.CSharp.CodeOps
{
    public class CSharpContractAuditor : IContractAuditor
    {
        readonly AbstractPolicy _defaultPolicy = new DefaultPolicy();
        readonly AbstractPolicy _systemPolicy = new SystemPolicy();
        
        private readonly AcsValidator _acsValidator = new AcsValidator();

        public int Category { get; } = 0;

        public IOptionsMonitor<CSharpCodeOpsOptions> CodeOpsOptionsMonitor { get; set; }

        public void Audit(byte[] code, RequiredAcs requiredAcs, bool isSystemContract)
        {
            var findings = new List<ValidationResult>();
            var policy = isSystemContract ? _systemPolicy : _defaultPolicy;
            var asm = Assembly.Load(code);
            var modDef = ModuleDefinition.ReadModule(new MemoryStream(code));
            var cts = new CancellationTokenSource(CodeOpsOptionsMonitor?.CurrentValue.AuditTimeoutDuration ??
                                                  Constants.DefaultAuditTimeoutDuration);

            // Check against whitelist
            findings.AddRange(policy.Whitelist.Validate(modDef, cts.Token));

            // Run module validators
            findings.AddRange(policy.ModuleValidators.SelectMany(v => v.Validate(modDef, cts.Token)));

            // Run assembly validators (run after module validators since we invoke BindService method below)
            findings.AddRange(policy.AssemblyValidators.SelectMany(v => v.Validate(asm, cts.Token)));

            // Run method validators
            foreach (var type in modDef.Types)
            {
                findings.AddRange(ValidateMethodsInType(policy, type, cts.Token));
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

        private IEnumerable<ValidationResult> ValidateMethodsInType(AbstractPolicy policy, TypeDefinition type,
            CancellationToken ct)
        {
            var findings = new List<ValidationResult>();

            foreach (var method in type.Methods)
            {
                findings.AddRange(policy.MethodValidators.SelectMany(v => v.Validate(method, ct)));
            }

            foreach (var nestedType in type.NestedTypes)
            {
                findings.AddRange(ValidateMethodsInType(policy, nestedType, ct));
            }

            return findings;
        }
    }
}