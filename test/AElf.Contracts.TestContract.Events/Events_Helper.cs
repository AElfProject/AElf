using AElf.Types;

namespace AElf.Contracts.TestContract.Events
{
    public partial class EventsContract
    {
        public Hash GetIssueOrderId(OrderInput input)
        {
            var hash1 = Hash.FromMessage(input);
            var hash2 = Context.TransactionId;

            return Hash.FromTwoHashes(hash1, hash2);
        }

        public void NewOrderId(Hash orderId)
        {
            var orderIds = State.OrderIds[OrderStatus.Created];
            if (orderIds == null)
            {
                State.OrderIds[OrderStatus.Created] = new OrderIds
                {
                    Ids = {orderId}
                };
                return;
            }
            
            orderIds.Ids.Add(orderId);
            State.OrderIds[OrderStatus.Created] = orderIds;
        }

        public void UpdateOrderId(Hash orderId, OrderStatus status)
        {
            var createdIds = State.OrderIds[OrderStatus.Created];
            Assert(createdIds != null, "no such status ids");
            Assert(createdIds.Ids.Contains(orderId), "Not exist such order id");
            
            //remove first
            createdIds.Ids.Remove(orderId);
            State.OrderIds[OrderStatus.Created] = createdIds;
            
            //then add
            var statusIds = State.OrderIds[status];
            if (statusIds == null)
            {
                State.OrderIds[status] = new OrderIds
                {
                    Ids = { orderId }
                };
            }
            else
            {
                statusIds.Ids.Add(orderId);
                State.OrderIds[status] = statusIds;
            }
        }

        public OrderInfo CheckDealOrderInput(Hash input)
        {
            var ids = State.OrderIds[OrderStatus.Created];
            Assert(ids.Ids.Contains(input), "Not contain such order or order completed or canceled.");
           
            var order = State.AllOrders[input];
           
            return order;
        }
    }
}