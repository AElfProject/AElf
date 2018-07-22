var AElf = require("aelf-sdk");
// This field will be injected from C#
// var aelf_config = new Object()
// aelf_config.server_addr = "http://localhost:5432"

aelf = new AElf(new AElf.providers.HttpProvider(aelf_config.server_addr));