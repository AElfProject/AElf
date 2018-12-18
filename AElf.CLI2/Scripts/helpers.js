function _getArrayLength(array) {
    return array.length;
}

function _getOwnPropertyNames(obj) {
    function onlyUnique(value, index, self) {
        return self.indexOf(value) === index;
    }

    var props = Object.getOwnPropertyNames(obj);
    if (obj.constructor !== undefined && obj.constructor.prototype !== undefined) {
        props = props.concat(Object.getOwnPropertyNames(obj.constructor.prototype));
    }

    return props.filter(onlyUnique).join(",");
}

function _getOwnProperty(obj, propertyName) {
    return obj[propertyName];
}

function _toString(v) {
    if (typeof v == 'object')
        return JSON.stringify(v);
    return v;
}

function _fromString(v) {
    return JSON.parse(v);
}

function _assignToUnderscore(obj) {
    _ = obj;
}

function _repeatedCalls(callback, milliSenconds, repetitions=0) {
    if (repetitions === 0) {
        repetitions = 2147483647; // int.MaxValue;
    }
    __repeatedCalls__(callback, milliSenconds, repetitions);
}

function _saveAccount(account, password) {
    var address = account.address;
    var privKey = account.privateKey;
    var pubKey = account.keyPair.getPublic('hex');
    __saveAccount__(address, privKey, pubKey, password);
}
