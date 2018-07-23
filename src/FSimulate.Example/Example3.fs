module Example3
open System
open System.Collections.Generic
open FSimulate

[<AllowNullLiteral>]
type Address (rng : Random) =
    member val X = rng.Next(100) with get, set
    member val Y = rng.Next(100) with get, set


[<AllowNullLiteral>]
type Product () =
    /// Gets or sets the reorder count (inventory level at which a reorder is triggered).
    member val ReorderCount = 0 with get, set
    /// Gets or sets the reorder amount (quantity to reorder).
    member val ReorderAmount = 0 with get, set
    /// Gets or sets the time required to reorder (in time periods)
    member val ReorderTime = 0 with get, set

[<AllowNullLiteral>]
type Order () =
    member val Product = Unchecked.defaultof<Product> with get, set
    member val Quantity = 0 with get, set
    member val DeliveryAddress = Unchecked.defaultof<Address> with get, set
    member val DeliveryTimePeriod = Unchecked.defaultof<Nullable<int>> with get, set


type OrderQueue = Queue<Order>


type WarehouseInventory (context) =
    inherit SimulationElement(context)
    let quantityByProduct = Dictionary<Product, int>()

    member this.Add (product : Product, quantity : int) =
        quantityByProduct.[product] <- this.CheckQuantity(product) + quantity

    member this.CanRemove(product : Product, quantity : int) =
        this.CheckQuantity(product) >= quantity

    member this.CheckQuantity (product : Product) =
        match quantityByProduct.TryGetValue(product) with
        | true, x -> x
        | _ -> 0

    member this.Remove (product : Product, quantity : int) =
        quantityByProduct.[product] <- this.CheckQuantity(product) - quantity



type ReorderProcess (context : ISimulationContext, product : Product) =
    inherit Process(context)

    override this.Simulate() = seq {
        // wait for the reorder time appropriate for the product
        yield upcast WaitInstruction product.ReorderTime;

        // increase the inventory level of this product
        let inventory = this.Context.GetByType<WarehouseInventory>() |> Seq.head
        inventory.Add(product, product.ReorderAmount);
    }



type DeliveryPerson (context, orderQueue : OrderQueue) =
    inherit Process(context)

    let mutable waitTime = 0L
    let mutable busyTime = 0L
    let mutable deliveryCount = 0
    let mutable latestDeliveryTime = 0L

    member this.WaitTime with get() = waitTime
    member this.BusyTime with get() = busyTime
    member this.DeliveryCount with get() = deliveryCount
    member this.LatestDeliveryTime with get() = latestDeliveryTime
    
    override this.Simulate () = seq {
        let inventory = this.Context.GetByType<WarehouseInventory>() |> Seq.head
        let mutable waitStart = this.Context.TimePeriod

        while orderQueue.Count > 0 do
            if this.Context.TimePeriod > waitStart then
                waitTime <- waitTime + (this.Context.TimePeriod - waitStart)
                waitStart <- this.Context.TimePeriod

            let mutable orderToDeliver = Unchecked.defaultof<Order>
            let mutable i = 0
            while i < orderQueue.Count && isNull orderToDeliver do
                let orderToCheck = orderQueue.Dequeue()
                if inventory.CanRemove(orderToCheck.Product, orderToCheck.Quantity) then
                    orderToDeliver <- orderToCheck
                else
                    // the order can't be filled yet.. push it to the end of the queue
                    orderQueue.Enqueue orderToCheck
                i <- i + 1

            if isNull orderToDeliver then
                // can't deliver anything now... pass
                yield upcast PassInstruction()
            else
                // remove from the warehouse
                inventory.Remove(orderToDeliver.Product, orderToDeliver.Quantity)

                // check the remaining quantity
                let quantity = inventory.CheckQuantity(orderToDeliver.Product)

                if quantity <= orderToDeliver.Product.ReorderCount && (quantity + orderToDeliver.Quantity) > orderToDeliver.Product.ReorderCount then
                    // the recent removal pushed levels below the reorder count
                    // start the reorder process
                    let reorder = ReorderProcess(this.Context, orderToDeliver.Product)
                    yield upcast ActivateInstruction reorder

                // deliver the order
                // work out how far to travel
                let mutable distanceToTravel =
                    (orderToDeliver.DeliveryAddress.X * orderToDeliver.DeliveryAddress.X) + (orderToDeliver.DeliveryAddress.Y * orderToDeliver.DeliveryAddress.Y)
                    |> float
                    |> Math.Sqrt
                    |> int

                // include travel back to warehouse
                distanceToTravel <- distanceToTravel * 2

                let timeToTravel = distanceToTravel / 10

                let deliveryStartTime = this.Context.TimePeriod
                // wait the delivery time
                yield upcast WaitInstruction timeToTravel
                let deliveryEndTime = this.Context.TimePeriod

                busyTime <- busyTime + (deliveryEndTime - deliveryStartTime)
                deliveryCount <- deliveryCount + 1
                waitStart <- this.Context.TimePeriod
                latestDeliveryTime <- this.Context.TimePeriod

    }

let randomSeed = 7

let run () =

    let createProducts () = [
        Product(ReorderCount = 90, ReorderAmount = 200, ReorderTime = 500)
        Product(ReorderCount = 110, ReorderAmount = 170, ReorderTime = 900)
        Product(ReorderCount = 45, ReorderAmount = 70, ReorderTime = 300)
    ]

    let createWarehouseInventory context products =
        let inventory = WarehouseInventory context
        for p in products do
            inventory.Add(p, p.ReorderCount * 2)
        inventory

    let generateOrders numberOfOrders products =
        let rand = Random(randomSeed)

        let productCount = List.length products
        let queue = OrderQueue()

        for i in 0 .. numberOfOrders-1 do
            let p = products |> List.item (rand.Next productCount)
            Order(Product = p, Quantity = rand.Next(10), DeliveryAddress = Address(Random(randomSeed)))
            |> queue.Enqueue
        
        queue

    let createDeliveryPeople context numberOfDeliveryPeople queue = seq {
        for i in 0 .. numberOfDeliveryPeople - 1 do
            yield DeliveryPerson(context, queue)
    }

    let outputResults (deliveryPeople : DeliveryPerson seq) =
        let mutable index = 1
        for dp in deliveryPeople do
            printfn "Delivery Person: %i, Deliveries: %i, Busy Time: %i, Wait Time: %i, Latest Delivery Time: %i"
                index dp.DeliveryCount dp.BusyTime dp.WaitTime dp.LatestDeliveryTime
            index <- index + 1

    use context = new SimulationContext()
    
    let products = createProducts ()
    
    let inventory = createWarehouseInventory context products
    context.Register inventory

    let orders = generateOrders 500 products
    let deliveryPeople = createDeliveryPeople context 3 orders |> Seq.toList

    // instantiate a new simulator
    let simulator = Simulator context

    // run the simulation
    simulator.Simulate()

    // output the statistics
    outputResults deliveryPeople

    ()
