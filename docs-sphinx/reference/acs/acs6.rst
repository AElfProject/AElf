ACS6 - Random Number Provider Standard
==================================

If your contract is about to generate a random number, you can consider using acs6.

Interface
---------

To provider a random number according to certain input, you only need to implement 
one interface:

Methods
~~~~~~~

+--------------------------------+----------------------------------------------------------------------------------+------------------------------------------------------------------+---------------------------------------------------+
| Method Name                    | Request Type                                                                     | Response Type                                                    | Description                                       |
+================================+==================================================================================+==================================================================+===================================================+
| RandomNumberProviderContract   | `google.protobuf.BytesValue <#google.protobuf.BytesValue>`__                     | `google.protobuf.BytesValue <#google.protobuf.BytesValue>`__     | Get the random number provided by this contract   |
+--------------------------------+----------------------------------------------------------------------------------+------------------------------------------------------------------+---------------------------------------------------+

Usage
-----

All you need is to override this method to return a random number according to the given input. 
You can decide the certain logic of generating random number yourself, just remember to return
a BytesValue type, thus the caller can deserialize the output himself.

Implementation
--------------

The easiest implementation：

.. code:: c#

    public override BytesValue GetRandomBytes(BytesValue input)
    {
        var serializedInput = new GetRandomBytesInput();
        serializedInput.MergeFrom(input.Value);
        var value = new Hash();
        value.MergeFrom(serializedInput.Value);
        var randomHashFromContext = Context.GetRandomHash(value);

        return new BytesValue
        {
            Value = serializedInput.Kind == 1
                ? new BytesValue {Value = randomHashFromContext.Value}.ToByteString()
                : new Int64Value {Value = Context.ConvertHashToInt64(randomHashFromContext, 1, 10000)}.ToByteString()
        };
    }
