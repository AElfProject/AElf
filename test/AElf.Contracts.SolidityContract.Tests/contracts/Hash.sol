pragma solidity >=0.8.2 <0.9.0;

contract Hash {

    // todo error
    // function HashSha256(string memory data) public view returns (bytes32){
    function HashSha256() public view returns (bytes32){
        bytes memory dataEncode;
        dataEncode = abi.encodePacked("hello");
        return sha256(abi.encodePacked(dataEncode));
    }

    function HashKeccak256() public view returns (bytes32){
        bytes memory dataEncode;
        dataEncode = abi.encodePacked("hello");
        return keccak256(abi.encodePacked(dataEncode));
    }
    
}