pragma solidity ^0.8.0;

contract GetCurrentAddress {
    function getAddress() public view returns (address) {
        assert(msg.sender == address(this));
        return address(this);
    }
}