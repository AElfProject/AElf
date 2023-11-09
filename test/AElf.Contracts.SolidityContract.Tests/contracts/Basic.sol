pragma solidity ^0.8.0;

contract Basic {

    function getAddress() public view returns (address) {
        return address(this);
    }

    function getBlockHeight() public view returns (uint64) {
        return block.number;
    }

    function hashSha256() public view returns (bytes32) {
        bytes memory dataEncode;
        dataEncode = abi.encodePacked("hello");
        return sha256(abi.encodePacked(dataEncode));
    }

    function hashKeccak256() public view returns (bytes32) {
        bytes memory dataEncode;
        dataEncode = abi.encodePacked("hello");
        return keccak256(abi.encodePacked(dataEncode));
    }
}