//var AElf = require("aelf-sdk");
//// This field will be injected from C#
//// var aelf_config = new Object()
//// aelf_config.server_addr = "http://localhost:5432"
//
//console.log("Connecting to ", aelf_config.server_addr);
//aelf = new AElf(new AElf.providers.HttpProvider(aelf_config.server_addr));


var fs = require("fs");
var fs_methods = Object.getOwnPropertyNames(fs).filter(function (p) {
    return typeof Math[p] === 'function';
});
console.log(fs_methods);