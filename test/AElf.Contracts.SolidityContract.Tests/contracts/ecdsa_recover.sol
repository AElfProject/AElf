pragma solidity ^0.8.20;

contract EcdsaRecover {

    function verify(bytes32 msgHash, uint8 v, bytes32 r, bytes32 s) public view returns (address){
        //return msgHash.toEthSignedMessageHash().recover(signature);
        return ecrecover(message, v, r, s);
    }
}