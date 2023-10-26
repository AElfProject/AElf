// https://solidity-by-example.org/delegatecall/

// SPDX-License-Identifier: MIT
// pragma solidity ^0.8.17;

contract Caller {
    address public callee;

    function testCall(address add) public{
        callee = add;
        callee.call(abi.encodeWithSignature("test()"));
    }

    function testDelegatecall(address add) public{
        callee = add;
        callee.delegatecall(abi.encodeWithSignature("test()"));
    }
}