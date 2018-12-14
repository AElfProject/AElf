(function () {
    deployCommand = function (category, code) {
        var txHash = chain.contractZero.DeploySmartContract(category, code).hash;
        console.log("Deploying contract ...");
        console.log("TxHash is: " + txHash);
        _repeatedCalls(function () {
            var res = aelf.chain.getTxResult(txHash).result;
            if (res.tx_status !== "Pending") {
                console.log("TxStatus is: " + res.tx_status);
            }
            if (res.tx_status === "Mined") {
                console.log("Address is: " + res.return);
            }
            return res.tx_status !== "Pending";
        }, 3000);
    };
})();