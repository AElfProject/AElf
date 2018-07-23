// This file will be inject by C#
// function _randomNextInt() {
//     return 0
// }

crypto = {
    getRandomValues: function (array) {
        if (array.constructor == Uint8Array) {
            for (var i = 0; i < array.length; ++i) {
                array[i] = _randomNextInt() % (1<<8);
            }
        } else {
            throw "not implemented";
        }
    }
};
global = {
    crypto: crypto
};