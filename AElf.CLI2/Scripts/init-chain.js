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

    function getAuthorization() {
        var cRes = aelf.chain.connectChain().result;
        var authorizationAddress = cRes['AElf.Contracts.Authorization'];
        //console.log('authorizationAddress ', authorizationAddress, '\n');
        return aelf.chain.contractAt(authorizationAddress, _account);
    }

    if (aelf.isConnected()) {
        chain = {
            contractZero: getContractZero(),
            authorizationContract: getAuthorization()
        };
    }
})();