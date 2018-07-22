var AElf = require("aelf-sdk");
var aelf_config = {
    "endpoint": "http://127.0.0.1:5500"
};
var aelf = new AElf(new AElf.providers.HttpProvider(aelf_config["endpoint"]));