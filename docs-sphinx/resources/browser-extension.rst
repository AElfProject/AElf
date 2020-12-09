aelf-web-extension
==================

You can get more information in
`Github <https://github.com/AElfProject/aelf-web-extension>`__

For User
--------

`release version, please waiting <#>`__

`dev
version <https://chrome.google.com/webstore/detail/aelf-explorer-extension-d/mlmlhipeonlflbcclinpbmcjdnpnmkpf>`__

If you are using qq browser,etc, you can add the extention too.

Notice
~~~~~~

.. note::

   Using File:/// protocol may can not use the extenstion
   // https://developer.chrome.com/extensions/match_patterns
   Note: Access to file URLs isn't automatic. The user must visit the extensions management page and opt in to file access for each extension that requests it.

For Dapp Developers
-------------------

Interaction Flow
~~~~~~~~~~~~~~~~

-  Make sure the user get the Extension
-  Connect Chain
-  Initialize Contract
-  Call contract methods

How to use
~~~~~~~~~~

If you need complete data structure. you can `click
here <#data-format>`__

-  `Check Extension Demo <#demo-of-checking-the-extension>`__
-  `GET_CHAIN_STATUS <#get-chain-status>`__
-  `CALL_AELF_CHAIN <#call-aelf-chain>`__
-  `LOGIN <#login>`__
-  `INIT_AELF_CONTRACT <#init-aelf-contract>`__
-  `CALL_AELF_CONTRACT /
   CALL_AELF_CONTRACT_READONLY <#call-aelf-contract-call-aelf-contract-readonly>`__
-  `CHECK_PERMISSION <#check-permission>`__
-  `SET_CONTRACT_PERMISSION <#set-contract-permission>`__
-  `REMOVE_CONTRACT_PERMISSION <#remove-contract-permission>`__
-  `REMOVE_METHODS_WHITELIST <#remove-methods-whitelist>`__

Data Format
-----------

.. code:: javascript

       NightElf = {
           histories: [],
           keychain: {
               keypairs: [
                   {
                       name: 'your keypairs name',
                       address: 'your keypairs address',
                       mnemonic: 'your keypairs mnemonic',
                       privateKey: 'your keupairs privateKey',
                       publicKey: {
                           x: 'you keupairs publicKey',
                           y: 'you keupairs publicKey'
                       }
                   }
               ],
               permissions: [
                   {
                       chainId: 'AELF',
                       contractAddress: 'contract address',
                       contractName: 'contract name',
                       description: 'contract description',
                       github: 'contract github',
                       whitelist: {
                           Approve: {
                               parameter1: 'a',
                               parameter2: 'b',
                               parameter3: 'c'
                           }
                       }
                   }
               ]
           }
       }

Demo of Checking the Extension
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

.. code:: js

   let nightElfInstance = null;
   class NightElfCheck {
       constructor() {
           const readyMessage = 'NightElf is ready';
           let resovleTemp = null;
           this.check = new Promise((resolve, reject) => {
               if (window.NightElf) {
                   resolve(readyMessage);
               }
               setTimeout(() => {
                   reject({
                       error: 200001,
                       message: 'timeout / can not find NightElf / please install the extension'
                   });
               }, 1000);
               resovleTemp = resolve;
           });
           document.addEventListener('NightElf', result => {
               console.log('test.js check the status of extension named nightElf: ', result);
               resovleTemp(readyMessage);
           });
       }
       static getInstance() {
           if (!nightElfInstance) {
               nightElfInstance = new NightElfCheck();
               return nightElfInstance;
           }
           return nightElfInstance;
       }
   }
   const nightElfCheck = NightElfCheck.getInstance();
   nightElfCheck.check.then(message => {
       // connectChain -> Login -> initContract -> call contract methods
   });

GET_CHAIN_STATUS
~~~~~~~~~~~~~~~~~~

You can see the demo
`./devDemos/test.html <https://github.com/hzz780/aelf-web-extension/tree/1.0/devDemos>`__.
[demo.js just a draft]

If you want to check Token Transfer Demo. You can `click
here <https://github.com/hzz780/aelf-web-extension/tree/master/demo/token>`__

The methods calls act the same as the methods call of the aelf-sdk.js

Note: ``'...'`` stands for omitted data.

.. code:: javascript

   const aelf = new window.NightElf.AElf({
       httpProvider: [
           'http://192.168.197.56:8101/chain',
           null,
           null,
           null,
           [{
               name: 'Accept',
               value: 'text/plain;v=1.0'
           }]
       ],
       appName: 'Test'
   });

   aelf.chain.getChainStatus((error, result) => {
       console.log('>>>>>>>>>>>>> connectChain >>>>>>>>>>>>>');
       console.log(error, result);
   });

   // result = {
   //     ChainId: "AELF"
   //     GenesisContractAddress: "61W3AF3Voud7cLY2mejzRuZ4WEN8mrDMioA9kZv3H8taKxF"
   // }

CALL_AELF_CHAIN
~~~~~~~~~~~~~~~~~

.. code:: javascript

   const txid = 'c45edfcca86f4f528cd8e30634fa4ac53801aae05365cfefc3bfe9b652fe5768';
   aelf.chain.getTxResult(txid, (err, result) => {
       console.log('>>>>>>>>>>>>> getTxResult >>>>>>>>>>>>>');
       console.log(err, result);
   });

   // result = {
   //     Status: "NotExisted"
   //     TransactionId: "ff5bcd126f9b7f22bbfd0816324390776f10ccb3fe0690efc84c5fcf6bdd3fc6"
   // }

LOGIN
~~~~~~~~

.. code:: javascript

   aelf.login({
       appName: 'hzzTest',
       chainId: 'AELF',
       payload: {
           method: 'LOGIN',
           contracts: [{
               chainId: 'AELF',
               contractAddress: '4rkKQpsRFt1nU6weAHuJ6CfQDqo6dxruU3K3wNUFr6ZwZYc',
               contractName: 'token',
               description: 'token contract',
               github: ''
           }, {
               chainId: 'AELF TEST',
               contractAddress: '2Xg2HKh8vusnFMQsHCXW1q3vys5JxG5ZnjiGwNDLrrpb9Mb',
               contractName: 'TEST contractName',
               description: 'contract description',
               github: ''
           }]
       }
   }, (error, result) => {
       console.log('login>>>>>>>>>>>>>>>>>>', result);
   });

   // keychain = {
   //     keypairs: [{
   //         name: 'your keypairs name',
   //         address: 'your keypairs address',
   //         mnemonic: 'your keypairs mnemonic',
   //         privateKey: 'your keypairs privateKey'，
   //         publicKey: {
   //             x: 'f79c25eb......',
   //             y: '7fa959ed......'
   //         }
   //     }],
   //     permissions: [{
   //         appName: 'hzzTest',
   //         address: 'your keyparis address',
   //         contracts: [{
   //             chainId: 'AELF',
   //             contractAddress: '4rkKQpsRFt1nU6weAHuJ6CfQDqo6dxruU3K3wNUFr6ZwZYc',
   //             contractName: 'token',
   //             description: 'token contract',
   //             github: ''
   //         }],
   //         domain: 'Dapp domain'
   //     }]
   // }

INIT_AELF_CONTRACT
~~~~~~~~~~~~~~~~~~~~

.. code:: javascript

   // In aelf-sdk.js wallet is the realy wallet.
   // But in extension sdk, we just need the address of the wallet.
   const tokenContract;
   const wallet = {
       address: '2JqnxvDiMNzbSgme2oxpqUFpUYfMjTpNBGCLP2CsWjpbHdu'
   };
   // It is different from the wallet created by Aelf.wallet.getWalletByPrivateKey();
   // There is only one value named address;
   aelf.chain.contractAtAsync(
       '4rkKQpsRFt1nU6weAHuJ6CfQDqo6dxruU3K3wNUFr6ZwZYc',
       wallet,
       (error, result) => {
           console.log('>>>>>>>>>>>>> contractAtAsync >>>>>>>>>>>>>');
           console.log(error, result);
           tokenContract = result;
       }
   );

   // result = {
   //     Approve: ƒ (),
   //     Burn: ƒ (),
   //     ChargeTransactionFees: ƒ (),
   //     ClaimTransactionFees: ƒ (),
   //     ....
   // }

CALL_AELF_CONTRACT / CALL_AELF_CONTRACT_READONLY
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

.. code:: javascript

   // tokenContract from the contractAsync
   tokenContract.GetBalance.call(
       {
           symbol: 'AELF',
           owner: '65dDNxzcd35jESiidFXN5JV8Z7pCwaFnepuYQToNefSgqk9'
       },
       (err, result) => {
           console.log('>>>>>>>>>>>>>>>>>>>', result);
       }
   );

   tokenContract.Approve(
       {
           symbol: 'AELF',
           spender: '4rkKQpsRFt1nU6weAHuJ6CfQDqo6dxruU3K3wNUFr6ZwZYc',
           amount: '100'
       },
       (err, result) => {
           console.log('>>>>>>>>>>>>>>>>>>>', result);
       }
   );

   // If you use tokenContract.GetBalance.call  this method is only applicable to queries that do not require extended authorization validation.(CALL_AELF_CONTRACT_READONLY)
   // If you use tokenContract.Approve this requires extended authorization validation (CALL_AELF_CONTRACT)

   // tokenContract.GetBalance.call(payload, (error, result) => {})
   // result = {
   //     symbol: "AELF",
   //     owner: "65dDNxzcd35jESiidFXN5JV8Z7pCwaFnepuYQToNefSgqk9",
   //     balance: 0
   // }

CHECK_PERMISSION
~~~~~~~~~~~~~~~~~~

.. code:: javascript

   aelf.checkPermission({
       appName: 'hzzTest',
       type: 'address', // if you did not set type, it aways get by domain.
       address: '4WBgSL2fSem9ABD4LLZBpwP8eEymVSS1AyTBCqXjt5cfxXK'
   }, (error, result) => {
       console.log('checkPermission>>>>>>>>>>>>>>>>>', result);
   });

   // result = {
   //     ...,
   //     permissions:[
   //         {
   //             address: '...',
   //             appName: 'hzzTest',
   //             contracts: [{
   //                 chainId: 'AELF',
   //                 contractAddress: '4rkKQpsRFt1nU6weAHuJ6CfQDqo6dxruU3K3wNUFr6ZwZYc',
   //                 contractName: 'token',
   //                 description: 'token contract',
   //                 github: ''
   //             },
   //             {
   //                 chainId: 'AELF TEST',
   //                 contractAddress: 'TEST contractAddress',
   //                 contractName: 'TEST contractName',
   //                 description: 'contract description',
   //                 github: ''
   //             }],
   //             domian: 'Dapp domain'
   //         }
   //     ]
   // }

SET_CONTRACT_PERMISSION
~~~~~~~~~~~~~~~~~~~~~~~~~

.. code:: javascript

   aelf.setContractPermission({
       appName: 'hzzTest',
       hainId: 'AELF',
       payload: {
           address: '2JqnxvDiMNzbSgme2oxpqUFpUYfMjTpNBGCLP2CsWjpbHdu',
           contracts: [{
               chainId: 'AELF',
               contractAddress: 'TEST contractAddress',
               contractName: 'AAAA',
               description: 'contract description',
               github: ''
           }]
       }
   }, (error, result) => {
       console.log('>>>>>>>>>>>>>', result);
   });

   // keychain = {
   //     keypairs: {...},
   //     permissions: [{
   //         appName: 'hzzTest',
   //         address: 'your keyparis address',
   //         contracts: [{
   //             chainId: 'AELF',
   //             contractAddress: '4rkKQpsRFt1nU6weAHuJ6CfQDqo6dxruU3K3wNUFr6ZwZYc',
   //             contractName: 'token',
   //             description: 'token contract',
   //             github: '',
   //             whitelist: {}
   //         },
   //         {
   //             chainId: 'AELF',
   //             contractAddress: 'TEST contractAddress',
   //             contractName: 'AAAA',
   //             description: 'contract description',
   //             github: ''
   //         }],
   //         domain: 'Dapp domain'
   //     }]
   // }

REMOVE_CONTRACT_PERMISSION
~~~~~~~~~~~~~~~~~~~~~~~~~~~~

.. code:: javascript

   aelf.removeContractPermission({
       appName: 'hzzTest',
       chainId: 'AELF',
       payload: {
           contractAddress: '2Xg2HKh8vusnFMQsHCXW1q3vys5JxG5ZnjiGwNDLrrpb9Mb'
       }
   }, (error, result) => {
       console.log('removeContractPermission>>>>>>>>>>>>>>>>>>>', result);
   });

   // keychain = {
   //     keypairs: {...},
   //     permissions: [{
   //         appName: 'hzzTest',
   //         address: 'your keyparis address',
   //         contracts: [{
   //             chainId: 'AELF',
   //             contractAddress: '4rkKQpsRFt1nU6weAHuJ6CfQDqo6dxruU3K3wNUFr6ZwZYc',
   //             contractName: 'token',
   //             description: 'token contract',
   //             github: ''
   //         }],
   //         domain: 'Dapp domain'
   //     }]
   // }

REMOVE_METHODS_WHITELIST
~~~~~~~~~~~~~~~~~~~~~~~~~~

.. code:: javascript

   aelf.removeMethodsWhitelist({
       appName: 'hzzTest',
       chainId: 'AELF',
       payload: {
           contractAddress: '2Xg2HKh8vusnFMQsHCXW1q3vys5JxG5ZnjiGwNDLrrpb9Mb',
           whitelist: ['Approve']
       }
   }, (error, result) => {
       console.log('removeWhitelist>>>>>>>>>>>>>>>>>', result);
   });
   // keychain = {
   //     keypairs: {...},
   //     permissions: [{
   //         appName: 'hzzTest',
   //         address: 'your keyparis address',
   //         contracts: [{
   //             chainId: 'AELF',
   //             contractAddress: '4rkKQpsRFt1nU6weAHuJ6CfQDqo6dxruU3K3wNUFr6ZwZYc',
   //             contractName: 'token',
   //             description: 'token contract',
   //             github: '',
   //             whitelist: {}
   //         }],
   //         domain: 'Dapp domain'
   //     }]
   // }

For Extension Developers
------------------------

1. Download the code

   .. code:: bash

      git clone https://github.com/hzz780/aelf-web-extension.git

2. Install dependent

   .. code:: bash

          npm install

3. Run webpack

   .. code:: bash

          webpack -w

4. Add to the browser

   .. code:: bash

          open development mode, add the webpack output app/public.

Project Information
-------------------

We use `ECDH <https://github.com/indutny/elliptic>`__ to use public key
to encryt data and private key to decrypt data.
