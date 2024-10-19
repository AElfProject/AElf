using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElf.Types;

namespace AElf.WebApp.Application.Chain.Dto;

[Display]
public class CreateRawTransactionInput : IValidatableObject
{
    /// <summary>
    ///     from address
    /// </summary>
    [Required]
    public string From { get; set; }

    /// <summary>
    ///     to address
    /// </summary>
    [Required]
    public string To { get; set; }

    /// <summary>
    ///     refer block height
    /// </summary>
    [Required]
    public long RefBlockNumber { get; set; }

    /// <summary>
    ///     refer block hash
    /// </summary>
    [Required]
    public string RefBlockHash { get; set; }

    /// <summary>
    ///     contract method name
    /// </summary>
    [Required]
    public string MethodName { get; set; }

    /// <summary>
    ///     contract method parameters
    /// </summary>
    [Required]
    public string Params { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var validationResults = new List<ValidationResult>();

        if (!TryParseAddress(From))
        {
            validationResults.Add(new ValidationResult(Error.Message[Error.InvalidAddress], new[] { nameof(From) }));
        }

        if (!TryParseAddress(To))
        {
            validationResults.Add(new ValidationResult(Error.Message[Error.InvalidAddress], new[] { nameof(To) }));
        }

        if (!TryParseHash(RefBlockHash))
        {
            validationResults.Add(new ValidationResult(Error.Message[Error.InvalidBlockHash],
                new[] { nameof(RefBlockHash) }));
        }

        return validationResults;
    }

    [ExceptionHandler(typeof(Exception), TargetType = typeof(CreateRawTransactionInput),
        MethodName = nameof(HandleExceptionWhileParsing))]
    private bool TryParseAddress(string base58Address)
    {
        Address.FromBase58(base58Address);
        return true;
    }

    [ExceptionHandler(typeof(Exception), TargetType = typeof(CreateRawTransactionInput),
        MethodName = nameof(HandleExceptionWhileParsing))]
    private bool TryParseHash(string hexHash)
    {
        Hash.LoadFromHex(hexHash);
        return true;
    }

    protected async Task<FlowBehavior> HandleExceptionWhileParsing(Exception ex)
    {
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = false
        };
    }
}