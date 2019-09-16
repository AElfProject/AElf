using AElf.Contracts.MultiToken;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestContract.Events
{
    public partial class EventsContract : EventsContractContainer.EventsContractBase
    {
        //action
        public override Empty InitializeEvents(InitializeInput input)
        {
            Assert(!State.Initialized.Value, "Already initialized.");

            State.Initialized.Value = true;

            return new Empty();
        }

        public override Empty IssueOrder(OrderInput input)
        {
            //sent token to contract first
            var hash = GetIssueOrderId(input);

            //transfer from
            State.TokenContract.TransferFrom.Send(new TransferFromInput
            {
                Symbol = input.SymbolPaid,
                Amount = input.BalancePaid,
                From = Context.Sender,
                To = Context.Self,
                Memo = "issue new order"
            });

            State.AllOrders[hash] = new OrderInfo
            {
                OrderId = hash,
                Issuer = Context.Sender,
                SymbolPaid = input.SymbolPaid,
                SymbolObtain = input.SymbolObtain,
                BalancePaid = input.BalancePaid,
                BalanceObtain = input.BalanceObtain,
                BalanceAchieved = 0,
                Status = OrderStatus.Created
            };

            Context.Fire(new OrderIssued
            {
                Issuer = Context.Sender,
                SymbolPaid = input.SymbolPaid,
                SymbolObtain = input.SymbolObtain,
                BalancePaid = input.BalancePaid,
                BalanceObtain = input.BalanceObtain
            });
            
            Context.Fire(new Transferred
            {
                From = Context.Sender,
                To = Context.Self,
                Symbol = input.SymbolPaid,
                Amount = input.BalancePaid,
                Memo = "Same name event for test" //same with token event transfer
            });
            return new Empty();
        }

        public override Empty DealOrder(DealOrderInput input)
        {
            var order = CheckDealOrderInput(input.OrderId);
            order.BalanceAchieved.Add(input.Amount);
            State.AllOrders[input.OrderId] = order;
            if (order.BalanceAchieved == order.BalanceObtain)
            {
                order.Status = OrderStatus.Done;
                State.AllOrders[input.OrderId] = order;

                UpdateOrderId(input.OrderId, OrderStatus.Done);
            }
            else
            {
                if (order.Status == OrderStatus.Created)
                {
                    order.Status = OrderStatus.Ongoing;
                    State.AllOrders[input.OrderId] = order;
                }
            }
            //add sub order record
            var subOrders = State.SubOrders[input.OrderId];
            subOrders.SubOrders.Add(new DealtOrder
            {
                Dealer = Context.Sender,
                Amount = input.Amount
            });

            //transfer from
            State.TokenContract.TransferFrom.Send(new TransferFromInput
            {
                Symbol = order.SymbolObtain,
                Amount = input.Amount,
                From = Context.Sender,
                To = Context.Self,
                Memo = "sent order token to contract first"
            });

            //transfer to issuer
            State.TokenContract.Transfer.Send(new TransferInput
            {
                Symbol = order.SymbolObtain,
                Amount = input.Amount,
                To = order.Issuer,
                Memo = "deal order"
            });

            //transfer to dealer
            var paidAmount = order.BalancePaid.Div(order.BalanceObtain).Mul(input.Amount);
            State.TokenContract.Transfer.Send(new TransferInput
            {
                Symbol = order.SymbolPaid,
                Amount = paidAmount,
                To = Context.Sender,
                Memo = "got token"
            });

            Context.Fire(new OrderDealt
            {
                OrderId = input.OrderId,
                Dealer = Context.Sender,
                Amount = input.Amount
            });

            return new Empty();
        }

        public override Empty CancelOrder(Hash input)
        {
            var order = CheckDealOrderInput(input);

            //update status
            order.Status = OrderStatus.Canceled;
            State.AllOrders[input] = order;
            UpdateOrderId(order.OrderId, OrderStatus.Canceled);

            //transfer back left token
            State.TokenContract.Transfer.Send(new TransferInput
            {
                Symbol = order.SymbolPaid,
                Amount = order.BalancePaid.Sub(order.BalanceAchieved),
                To = order.Issuer,
                Memo = "redeem back margin token"
            });

            Context.Fire(new OrderCanceled
            {
                OrderId = input
            });

            return new Empty();
        }
    }
}