// https://solidity-by-example.org/delegatecall/

// SPDX-License-Identifier: MIT
// pragma solidity ^0.8.17;

// NOTE: Deploy this contract first
contract Delegatee {
    // NOTE: storage layout must be the same as contract Delegator
    uint public num;
    address public sender;
    uint public value;

    function setVars(uint _num) public payable {
        num = _num;
        sender = msg.sender;
        value = msg.value;
    }
}