using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AElf.Types;

namespace AElf.WebApp.Application.Chain.Dto
{
    [Display]
    public class CreateRawTransactionInput : IValidatableObject
    {
        /// <summary>
        /// from address
        /// </summary>
        [Required] 
        public string From { get; set; }

        /// <summary>
        /// to address
        /// </summary>
        [Required] 
        public string To { get; set; }

        /// <summary>
        /// refer block height
        /// </summary>
        [Required] 
        public long RefBlockNumber { get; set; }

        /// <summary>
        /// refer block hash
        /// </summary>
        [Required] 
        public string RefBlockHash { get; set; }

        /// <summary>
        /// contract method name
        /// </summary>
        [Required] 
        public string MethodName { get; set; }

        /// <summary>
        /// contract method parameters
        /// </summary>
        [Required] 
        public string Params { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var validationResults = new List<ValidationResult>();
            try
            {
                AddressHelper.Base58StringToAddress(From);
            }
            catch
            {
                validationResults.Add(new ValidationResult(Error.Message[Error.InvalidAddress],
                    new[] {nameof(From)}));
            }

            try
            {
                AddressHelper.Base58StringToAddress(To);
            }
            catch
            {
                validationResults.Add(
                    new ValidationResult(Error.Message[Error.InvalidAddress], new[] {nameof(To)}));
            }

            try
            {
                HashHelper.HexStringToHash(RefBlockHash);
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