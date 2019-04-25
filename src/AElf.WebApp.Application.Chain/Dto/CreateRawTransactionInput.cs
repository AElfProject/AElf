using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AElf.WebApp.Application.Chain.Dto
{
    [Display]
    public class CreateRawTransactionInput : IValidatableObject
    {
        [Required] 
        public string From { get; set; }

        [Required] 
        public string To { get; set; }

        [Required] 
        public long RefBlockNumber { get; set; }

        [Required] 
        public string RefBlockHash { get; set; }

        [Required] 
        public string MethodName { get; set; }

        [Required] 
        public string Params { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var validationResults = new List<ValidationResult>();
            try
            {
                Address.Parse(From);
            }
            catch
            {
                validationResults.Add(new ValidationResult(Error.Message[Error.InvalidAddress],
                    new[] {nameof(From)}));
            }

            try
            {
                Address.Parse(To);
            }
            catch
            {
                validationResults.Add(
                    new ValidationResult(Error.Message[Error.InvalidAddress], new[] {nameof(To)}));
            }

            try
            {
                Hash.LoadHex(RefBlockHash);
            }
            catch
            {
                validationResults.Add(
                    new ValidationResult(Error.Message[Error.InvalidBlockHash], new[] {nameof(RefBlockHash)}));
            }
            return validationResults;
        }
    }

}