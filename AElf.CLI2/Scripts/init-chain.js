(function () {
    function getContractZero() {
        var cRes = aelf.chain.connectChain().result;
        var addressZero = cRes.BasicContractZero;
        if (typeof addressZero === 'undefined') {
            addressZero = cRes['AElf.Contracts.Genesis'];
        }
        if(typeof addressZero === 'undefined'){
            throw "Cannot find contract zero's address.";
        }
        return aelf.chain.contractAt(addressZero, _account);
    }

    if (aelf.isConnected()) {
        chain = {
            contractZero: getContractZero()
        };
    }
})();