// https://solidity-by-example.org/delegatecall/

// SPDX-License-Identifier: MIT
// pragma solidity ^0.8.17;

// NOTE: Deploy this contract after Delegatee contract.
contract Delegator {
    uint public num;
    address public sender;
    uint public value;

    function setVars(address _contract, uint _num) public payable {
        // Delegatee's storage is set, Delegator is not modified.
        (bool success, bytes memory data) = _contract.delegatecall(
            abi.encodeWithSignature("setVars(uint256)", _num)
        );
        require(success);
        require(data.length == 0);
    }
}