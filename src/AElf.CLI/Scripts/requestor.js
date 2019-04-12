(function () {
    _requestor.isConnected = function () {
        try {
            var res = this.send({
                id: 1234,
                jsonrpc: '2.0',
                method: 'GetChainInformation',
                params: {}
            });
            return res && res.error === undefined;
        } catch (e) {
            return false;
        }
    }
})();