(function () {
    _requestor.isConnected = function () {
        try {
            var res = this.send({
                id: 9999,
                jsonrpc: '2.0',
                method: 'connect_chain',
                params: {}
            });
            return res && res.error === undefined;
        } catch (e) {
            return false;
        }
    }
})();