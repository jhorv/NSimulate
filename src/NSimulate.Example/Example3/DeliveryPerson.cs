using System;
using System.Collections.Generic;
using System.Linq;
using NSimulate;
using NSimulate.Instruction;

namespace NSimulate.Example3
{
    /// <summary>
    /// The process of delivery perfomed by a delivery person.
    /// </summary>
    public class DeliveryPerson : Process
    {
        private OrderQueue _orderQueue;

        /// <summary>
        /// Initializes a new instance of the <see cref="NSimulate.Example3.DeliveryPerson"/> class.
        /// </summary>
        /// <param name='orderQueue'>
        /// Order queue.
        /// </param>
        public DeliveryPerson(SimulationContext context, OrderQueue orderQueue)
            : base(context)
        {
            _orderQueue = orderQueue;
        }

        /// <summary>
        /// Gets the cumulative wait time (in time periods) that this delivery person has waited
        /// </summary>
        public long WaitTime { get; private set; }

        /// <summary>
        /// Gets the busy time (in time periods) of this delivery person.
        /// </summary>
        public long BusyTime { get; private set; }

        /// <summary>
        /// Gets the delivery count (number of orders delivered).
        /// </summary>
        public int DeliveryCount { get; private set; }

        /// <summary>
        /// Gets the latest delivery time.
        /// </summary>
        public long LatestDeliveryTime { get; private set; }

        /// <summary>
        ///  Simulate the process. 
        /// </summary>
        public override IEnumerator<InstructionBase> Simulate()
        {
            WarehouseInventory inventory = Context.GetByType<WarehouseInventory>().First();

            var waitStart = Context.TimePeriod;

            while (_orderQueue.Count > 0)
            {
                if (Context.TimePeriod > waitStart)
                {
                    WaitTime += (Context.TimePeriod - waitStart);
                    waitStart = Context.TimePeriod;
                }

                Order orderToDeliver = null;

                var i = 0;
                while (i < _orderQueue.Count && orderToDeliver == null)
                {
                    Order orderToCheck = _orderQueue.Dequeue();
                    if (inventory.CanRemove(orderToCheck.Product, orderToCheck.Quantity))
                    {
                        orderToDeliver = orderToCheck;
                    }
                    else
                    {
                        // the order can't be filled yet.. push it to the end of the queue
                        _orderQueue.Enqueue(orderToCheck);
                    }
                    i++;
                }

                if (orderToDeliver == null)
                {
                    // can't deliver anything now... pass
                    yield return new PassInstruction();
                }
                else
                {
                    // remove from the warehouse
                    inventory.Remove(orderToDeliver.Product, orderToDeliver.Quantity);

                    // check the remaining quantity
                    var quantity = inventory.CheckQuantity(orderToDeliver.Product);

                    if (quantity <= orderToDeliver.Product.ReorderCount
                        && (quantity + orderToDeliver.Quantity) > orderToDeliver.Product.ReorderCount)
                    {
                        // the recent removal pushed levels below the reorder count
                        // start the reorder process
                        var reorder = new ReorderProcess(Context, orderToDeliver.Product);
                        yield return new ActivateInstruction(reorder);
                    }
                    // deliver the order
                    // work out how far to travel
                    var distanceToTravel = (int)Math.Sqrt((orderToDeliver.DeliveryAddress.X * orderToDeliver.DeliveryAddress.X)
                                                     + (orderToDeliver.DeliveryAddress.Y * orderToDeliver.DeliveryAddress.Y));

                    // include travel back to warehouse
                    distanceToTravel = distanceToTravel * 2;

                    var timeToTravel = distanceToTravel / 10;

                    var deliveryStartTime = Context.TimePeriod;
                    // wait the delivery time
                    yield return new WaitInstruction(timeToTravel);
                    var deliveryEndTime = Context.TimePeriod;

                    BusyTime += (deliveryEndTime - deliveryStartTime);
                    DeliveryCount++;
                    waitStart = Context.TimePeriod;
                    LatestDeliveryTime = Context.TimePeriod;
                }
            }
        }
    }
}

