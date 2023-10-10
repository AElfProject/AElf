contract balances {
    constructor() payable {}

    function get_balance() public view returns (uint128) {
        return address(this).balance;
    }

    function transfer(address payable addr, uint128 amount) public {
        addr.transfer(amount);
    }

    function send(address payable addr, uint128 amount) public returns (bool) {
        return addr.send(amount);
    }

    function pay_me() public payable {
        uint128 v = msg.value;

        print("Thank you very much for {}".format(v));
    }
}
