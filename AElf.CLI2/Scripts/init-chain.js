(function () {
    function getContractZero() {
        var addressZero = aelf.chain.connectChain().result.BasicContractZero;
        var contractZero = aelf.chain.contractAt(addressZero, _account);
        return contractZero;
    }

    if (aelf.isConnected()) {
        chain = {
            contractZero: getContractZero()
        };
    }
})();