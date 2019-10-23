using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestContract.Events
{
    public partial class EventsContract
    {
        //view
        public override OrdersOutput QueryIssueOrders(Empty input)
        {
            var orders = new OrdersOutput();

            var createOrders = State.OrderIds[OrderStatus.Created];
            if (createOrders != null && createOrders.Ids.Count > 0)
            {
                foreach (var id in createOrders.Ids)
                {
                    var order = State.AllOrders[id];
                    orders.Orders.Add(order);
                }
            }

            var ongoingOrders = State.OrderIds[OrderStatus.Ongoing];
            if (ongoingOrders == null || ongoingOrders.Ids.Count <= 0) return orders;
            
            foreach (var id in ongoingOrders.Ids)
            {
                var order = State.AllOrders[id];
                orders.Orders.Add(order);
            }

            return orders;
        }

        public override OrdersOutput QueryDoneOrders(Empty input)
        {
            var orders = new OrdersOutput();

            var doneOrders = State.OrderIds[OrderStatus.Done];
            if (doneOrders == null || doneOrders.Ids.Count <= 0) return orders;

            foreach (var id in doneOrders.Ids)
            {
                var order = State.AllOrders[id];
                orders.Orders.Add(order);
            }

            return orders;
        }

        public override OrdersOutput QueryCanceledOrders(Empty input)
        {
            var orders = new OrdersOutput();

            var cancelOrders = State.OrderIds[OrderStatus.Canceled];
            if (cancelOrders == null || cancelOrders.Ids.Count <= 0) return orders;

            foreach (var id in cancelOrders.Ids)
            {
                var order = State.AllOrders[id];
                orders.Orders.Add(order);
            }

            return orders;
        }

        public override OrderInfo QueryOrderById(Hash input)
        {
            var order = State.AllOrders[input];
            return order ?? new OrderInfo();
        }

        public override DealtOrders QueryOrderSubOrders(Hash input)
        {
            var orders = State.SubOrders[input];

            return orders ?? new DealtOrders();
        }
    }
}