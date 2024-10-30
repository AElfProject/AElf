using AElf.Runtime.WebAssembly.TransactionPayment;
using AElf.Runtime.WebAssembly.TransactionPayment.Extensions;
using Google.Protobuf;

namespace AElf.Runtime.WebAssembly.Contract;

public partial class WebAssemblyContractImplementation
{
    public bool EstimateGas { get; set; }

    public Weight? ChargeGas(RuntimeCost runtimeCost)
    {
        return new Weight();
        var gasLeft = GasMeter?.ChargeGas(runtimeCost);
        if (gasLeft != null && !EstimateGas && gasLeft.Insufficient())
        {
            //HandleError(WebAssemblyError.OutOfGas);
        }

        return gasLeft;
    }

    /// <summary>
    /// Stores the price for the specified amount of gas into the supplied buffer.
    ///
    /// Equivalent to the newer [`seal1`][`super::api_doc::Version2::weight_to_fee`] version but
    /// works with *ref_time* Weight only. It is recommended to switch to the latest version, once
    /// it's stabilized.
    /// </summary>
    /// <param name="gas"></param>
    /// <param name="outPtr"></param>
    /// <param name="outLenPtr"></param>
    private void WeightToFeeV0(long gas, int outPtr, int outLenPtr)
    {
        WriteSandboxOutput(outPtr, outLenPtr,
            new FeeService(new IFeeProvider[]
            {
                new LengthFeeProvider(),
                new WeightFeeProvider(new FeeFunctionProvider())
            }).CalculateFees(new Weight(gas, 0)));
    }

    /// <summary>
    /// Stores the price for the specified amount of weight into the supplied buffer.
    ///
    /// # Parameters
    ///
    /// - `out_ptr`: pointer to the linear memory where the returning value is written to. If the
    ///   available space at `out_ptr` is less than the size of the value a trap is triggered.
    /// - `out_len_ptr`: in-out pointer into linear memory where the buffer length is read from and
    ///   the value length is written to.
    ///
    /// The data is encoded as `T::Balance`.
    ///
    /// # Note
    ///
    /// It is recommended to avoid specifying very small values for `ref_time_limit` and
    /// `proof_size_limit` as the prices for a single gas can be smaller than the basic balance
    /// unit.
    /// </summary>
    /// <param name="refTimeLimit"></param>
    /// <param name="proofSizeLimit"></param>
    /// <param name="outPtr"></param>
    /// <param name="outLenPtr"></param>
    private void WeightToFeeV1(long refTimeLimit, long proofSizeLimit, int outPtr, int outLenPtr)
    {
        WriteSandboxOutput(outPtr, outLenPtr,
            new FeeService(new IFeeProvider[]
            {
                new LengthFeeProvider(),
                new WeightFeeProvider(new FeeFunctionProvider())
            }).CalculateFees(new Weight(refTimeLimit, proofSizeLimit)));
    }

    /// <summary>
    /// Stores the weight left into the supplied buffer.
    ///
    /// Equivalent to the newer [`seal1`][`super::api_doc::Version2::gas_left`] version but
    /// works with *ref_time* Weight only. It is recommended to switch to the latest version, once
    /// it's stabilized.
    /// </summary>
    /// <param name="outPtr"></param>
    /// <param name="outLenPtr"></param>
    private void GasLeftV0(int outPtr, int outLenPtr)
    {
        var gasLeft = GasMeter?.GasLeft;
        WriteSandboxOutput(outPtr, outLenPtr, gasLeft.ToByteArray());
    }

    /// <summary>
    /// Stores the amount of weight left into the supplied buffer.
    ///
    /// The value is stored to linear memory at the address pointed to by `out_ptr`.
    /// `out_len_ptr` must point to a u32 value that describes the available space at
    /// `out_ptr`. This call overwrites it with the size of the value. If the available
    /// space at `out_ptr` is less than the size of the value a trap is triggered.
    ///
    /// The data is encoded as Weight.
    /// </summary>
    /// <param name="outPtr"></param>
    /// <param name="outLenPtr"></param>
    private void GasLeftV1(int outPtr, int outLenPtr)
    {
        var gasLeft = GasMeter?.GasLeft;
        WriteSandboxOutput(outPtr, outLenPtr, gasLeft.ToByteArray());
    }
}