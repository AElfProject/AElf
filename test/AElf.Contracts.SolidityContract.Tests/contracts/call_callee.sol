// https://solidity-by-example.org/delegatecall/

// SPDX-License-Identifier: MIT
// pragma solidity ^0.8.17;

// NOTE: Deploy this contract first
contract Callee {
    address public add;

    function test() public returns (address output){
        output = address(this);
        add = output;
    }
}