(function () {
    deployCommand = function (category, code) {
        return new Promise(function (resolve, reject) {

            var contractAddress = null;
            var getResult = null;
            var res = null;
            getResult = function (txHash) {
                res = aelf.chain.getTxResult(txHash).result;
                if (res.tx_status === "Pending") {
                    timer.setTimeout(function () {
                        getResult(txHash);
                    }, 3000);
                } else {
                    console.log("  " + res.tx_status);
                    if (res.tx_status === "Mined") {
                        console.log("Address is: " + res.return);
                        contractAddress = res.return;
                    }
                    resolve(1);
                }
            };
            var txHash = chain.contractZero.DeploySmartContract(category, code).hash;
            console.log("Deploying contract ...\n  tx hash is " + txHash);
            getResult(txHash);
        });

    };
})();