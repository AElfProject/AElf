pragma solidity ^0.8.18;

contract EcdsaRecover {

    function verify(bytes32 msgHash, uint8 v, bytes32 r, bytes32 s) public pure returns (address) {
        //return msgHash.toEthSignedMessageHash().recover(signature);
        return ecrecover(msgHash, v, r, s);
    }
}