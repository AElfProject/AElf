pragma solidity ^0.8.20;

contract Now {

    // Function to get the current count
    function now() public view returns (uint){
        return block.timestamp;
    }

    function caller() public view returns (address){
        return msg.sender;
    }

}