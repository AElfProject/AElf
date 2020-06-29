## keypairs and addresses

You can easily create a key pair with our **command line tool** with the **create** command.

``` shell
aelf-command create
```

Creation will be successful after you provide a valid passwrod. When creating the key-pair (that we sometimes refer to as the "account") it will generate a file with the ".json" extension. This file will contain the public and private key and will be encrypted with the password you provided before.

If you are writing a dApp you can also use the following method in the js-sdk, it is based on bip39 for generating a deterministic key pair with a "mnemonic sentence" ([here for more on this](https://github.com/bitcoin/bips/blob/master/bip-0039.mediawiki)):

```js
import Aelf from 'aelf-sdk';
Aelf.wallet.createNewWallet();
```

This will return an object containing the mnemonic used, the key-pair and the address. In AElf we usually encode the address in base58. This address is derived from the public, we calculate it as the first 30 bytes of the double sha256 hash. The AElf js-sdk provides the following, that returns the address:

```js
import Aelf from 'aelf-sdk';
const address = aelf.wallet.getAddressFromPubKey(pubKey);
```

For more information about how the js-sdk works, please refer to the [SDK reference](../sdk/javascript/js-sdk.md).

Finally here is the Protobuf message we use for representing an address, it is often used by other types to represent addresses:

```Proto
option csharp_namespace = "AElf.Types";
message Address
{
    bytes value = 1;
}
```

To sum up, if you need an AElf key-pair, you can either directly use the **create** command or use the js-sdk. Note that this key-pair files should be located in the keys folder of the data directory (datadir). You can read more about this in the [getting started](../tutorials/run-node.md) section.