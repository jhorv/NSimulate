module Example2
open System
open System.Collections.Generic
open FSharp.Core.Operators
open FSimulate

let private nullInt64 = Unchecked.defaultof<Nullable<int64>>

type Level1CallCenterStaffMember (context) =
    inherit Resource(context)

type Level2CallCenterStaffMember (context) =
    inherit Resource(context)

/// Characteristics describing attributes of a call
type CallCharacteristics () =
    member val TimeBeforeCallStarts = 0 with get, set
    member val CallDurationAtLevel1 = 0 with get, set
    member val CallDurationAtLevel2 = 0 with get, set
    member val CallIsEscalatedToLevel2 = false with get, set

/// Statistics captured for a call
type CallStatistics () =

    let calculateTimeDifference (start : Nullable<Int64>, ``end`` : Nullable<Int64>) =
        let result =
            if start.HasValue && ``end``.HasValue then
                ``end``.Value - start.Value
            else
                0L
        Nullable result

    member val CallStartTimePeriod = nullInt64 with get, set
    member val CallLevel1TimePeriod = nullInt64 with get, set
    member val CallLevel1EndTimePeriod = nullInt64 with get, set
    member val CallLevel2TimePeriod = nullInt64 with get, set
    member val CallLevel2EndTimePeriod = nullInt64 with get, set
    member val CallEndTimePeriod = nullInt64 with get, set

    member this.TimeOnHoldBeforeLevel1
        with get() = calculateTimeDifference(this.CallStartTimePeriod, this.CallLevel1TimePeriod)
    
    member this.TimeOnHoldBeforeLevel2
        with get() = calculateTimeDifference(this.CallLevel1EndTimePeriod, this.CallLevel2TimePeriod)

    member this.TimeAtLevel1
        with get() = calculateTimeDifference(this.CallLevel1TimePeriod, this.CallLevel1EndTimePeriod)

    member this.TimeAtLevel2
        with get() = calculateTimeDifference(this.CallLevel2TimePeriod, this.CallLevel2EndTimePeriod)
    
    member this.TotalTimeOnHold
        with get() = Nullable (this.TimeOnHoldBeforeLevel1.GetValueOrDefault() +
                        this.TimeOnHoldBeforeLevel2.GetValueOrDefault())
    
    member this.TotalTimeOfCall
        with get() = calculateTimeDifference(this.CallStartTimePeriod, this.CallEndTimePeriod)

type Call (context, characteristics : CallCharacteristics) =
    inherit Process(context)

    member val Characteristics = characteristics with get, set
    member val Statistics = CallStatistics () with get, set

    override this.Simulate() = seq {
        if this.Characteristics.TimeBeforeCallStarts > 0 then
            // wait before the call starts
            yield upcast WaitInstruction this.Characteristics.TimeBeforeCallStarts

        // record the time period in which the call is tarted
        this.Statistics.CallStartTimePeriod <- Nullable this.Context.TimePeriod

        // the call is answered by a call center worker
        // the time rquired to obtain this resource is the time the customer is on hold
        let level1StaffMemberAllocateInstruction = AllocateInstruction<Level1CallCenterStaffMember> 1
        yield upcast level1StaffMemberAllocateInstruction

        this.Statistics.CallLevel1TimePeriod <- Nullable this.Context.TimePeriod

        // keep hold of the resource for the call duration... this represents the time the customer spends with the call center worker at level 1
        yield upcast WaitInstruction this.Characteristics.CallDurationAtLevel1

        // Level 1 portion of the call is ended
        this.Statistics.CallLevel1EndTimePeriod <- Nullable this.Context.TimePeriod

        // release the first call resource
        yield upcast ReleaseInstruction<Level1CallCenterStaffMember> level1StaffMemberAllocateInstruction

        if this.Characteristics.CallIsEscalatedToLevel2 then
            // if the call is elevated to level 2
            // request a level 2 call center responder
            // the time taken to obtain this resource is the time the customer spends on hold in the second queue
            let level2StaffMemberAllocateInstruction = AllocateInstruction<Level2CallCenterStaffMember> 1
            yield upcast level2StaffMemberAllocateInstruction

            this.Statistics.CallLevel2TimePeriod <- Nullable this.Context.TimePeriod

            // hold the resource for the call duration at level 2...
            yield upcast WaitInstruction this.Characteristics.CallDurationAtLevel2

            // Level 2 portion of the call is ended
            this.Statistics.CallLevel2EndTimePeriod <- Nullable this.Context.TimePeriod

            // release the second call resource
            yield upcast ReleaseInstruction<Level2CallCenterStaffMember> level2StaffMemberAllocateInstruction

        // the call is complete
        this.Statistics.CallEndTimePeriod <- Nullable this.Context.TimePeriod
    }

let generatePhoneCalls
    (context : ISimulationContext
    , numberOfCalls : int
    , level1CallTime : int
    , level2CallTime : int
    , callTimeVariability : float
    , callStartTimeRange : int
    , rand : Random) =

    let calls = List<Call>()
    for i in 0 .. (numberOfCalls - 1) do
        let characteristics = CallCharacteristics ()

        characteristics.TimeBeforeCallStarts <- rand.Next(callStartTimeRange)

        // for half the calls, force them to be in a peak period in the middle of the range
        if i < numberOfCalls / 2 then
            characteristics.TimeBeforeCallStarts <- (int)((float characteristics.TimeBeforeCallStarts / 3.0) + (float callStartTimeRange / 3.0))

        characteristics.CallDurationAtLevel1 <- (int)
            (level1CallTime
                + rand.Next((int)(float level1CallTime * callTimeVariability))
                - (int)((float level1CallTime * callTimeVariability) / 2.0))

        characteristics.CallIsEscalatedToLevel2 <- rand.Next(1000) > 500
        if characteristics.CallIsEscalatedToLevel2 then
            characteristics.CallDurationAtLevel2 <- (int)
                (level1CallTime
                    + rand.Next((int)(float level1CallTime * callTimeVariability))
                    - (int)((float level1CallTime * callTimeVariability) / 2.0))

        let newCall = Call(context, characteristics)
        calls.Add newCall
    calls

let outputResults (calls : IEnumerable<Call>) =
    let mutable countOfCalls = 0
    let mutable countOfCallsReachingLevel2 = 0
    let mutable totalLevel1Duration = 0L
    let mutable totalLevel2Duration  = 0L
    let mutable totalHoldTimeBeforeLevel1 = 0L
    let mutable totalHoldTimeBeforeLevel2 = 0L
    let mutable totalCallTime = 0L
    let mutable totalHoldTime = 0L
    let mutable maxHoldTimeBeforeLevel1 = nullInt64
    let mutable maxHoldTimeBeforeLevel2 = nullInt64
    let mutable maxTotalHoldTime = nullInt64
    let mutable maxTotalCallTime = nullInt64

    for call in calls do
        countOfCalls <- countOfCalls + 1

        if call.Statistics.CallLevel2TimePeriod.HasValue then
            countOfCallsReachingLevel2 <- countOfCallsReachingLevel2 + 1

        totalLevel1Duration <- totalLevel1Duration + call.Statistics.TimeAtLevel1.GetValueOrDefault()
        totalLevel2Duration <- totalLevel2Duration + call.Statistics.TimeAtLevel2.GetValueOrDefault()
        totalHoldTimeBeforeLevel1 <- totalHoldTimeBeforeLevel1 + call.Statistics.TimeOnHoldBeforeLevel1.GetValueOrDefault()
        totalHoldTimeBeforeLevel2 <- totalHoldTimeBeforeLevel2 + call.Statistics.TimeOnHoldBeforeLevel2.GetValueOrDefault()
        totalCallTime <- totalCallTime + call.Statistics.TotalTimeOfCall.GetValueOrDefault()
        totalHoldTime <- totalHoldTime + call.Statistics.TotalTimeOnHold.GetValueOrDefault()

        if maxHoldTimeBeforeLevel1.HasValue = false || call.Statistics.TimeOnHoldBeforeLevel1.GetValueOrDefault() > maxHoldTimeBeforeLevel1.Value then
            maxHoldTimeBeforeLevel1 <- call.Statistics.TimeOnHoldBeforeLevel1

        if maxHoldTimeBeforeLevel2.HasValue = false || call.Statistics.TimeOnHoldBeforeLevel2.GetValueOrDefault() > maxHoldTimeBeforeLevel2.Value then
            maxHoldTimeBeforeLevel2 <- call.Statistics.TimeOnHoldBeforeLevel2

        if maxTotalHoldTime.HasValue = false || call.Statistics.TotalTimeOnHold.GetValueOrDefault() > maxTotalHoldTime.Value then
            maxTotalHoldTime <- call.Statistics.TotalTimeOnHold

        if maxTotalCallTime.HasValue = false || call.Statistics.TotalTimeOfCall.GetValueOrDefault() > maxTotalCallTime.Value then
            maxTotalCallTime <- call.Statistics.TotalTimeOfCall


    if countOfCalls > 0 then
        Console.WriteLine("------------------------------------------------------")
        Console.WriteLine("Number of calls                 : {0}", countOfCalls)
        Console.WriteLine("Average hold time before level 1: {0} seconds", totalHoldTimeBeforeLevel1 / int64 countOfCalls)
        Console.WriteLine("Maximum hold time before level 1: {0} seconds", maxHoldTimeBeforeLevel1)
        Console.WriteLine("Average call time at level 1    : {0} seconds", totalLevel1Duration / int64 countOfCalls)
        Console.WriteLine("------------------------------------------------------")
        if countOfCallsReachingLevel2 > 0 then
            Console.WriteLine("Number of calls reaching level 2: {0}", countOfCallsReachingLevel2)
            Console.WriteLine("Average hold time before level 2: {0} seconds", totalHoldTimeBeforeLevel2 / int64 countOfCallsReachingLevel2)
            Console.WriteLine("Maximum hold time before level 2: {0} seconds", maxHoldTimeBeforeLevel2)
            Console.WriteLine("Average call time at level 2    : {0} seconds", totalLevel2Duration / int64 countOfCallsReachingLevel2)
            Console.WriteLine("------------------------------------------------------")
        Console.WriteLine("Total call time                 : {0} seconds", totalCallTime)
        Console.WriteLine("Total hold time                 : {0} seconds", totalHoldTime)
        Console.WriteLine("Average call time               : {0} seconds", totalCallTime / int64 countOfCalls)
        Console.WriteLine("Average hold time               : {0} seconds", totalHoldTime / int64 countOfCalls)
        Console.WriteLine("------------------------------------------------------");
    
    ()



let run () =
    use context = new SimulationContext()

    // Add the resources that represent the staff of the call center
    context.Register(Level1CallCenterStaffMember(context, Capacity = 10))
    context.Register(Level2CallCenterStaffMember(context, Capacity = 5))

    let rng = Random(7)

    // Add the processes that represent the phone calls to the call center
    let calls = generatePhoneCalls(context, 500, 120, 300, 0.5, 14400, rng)
                
    // instantate a new simulator
    let simulator = Simulator context

    // run the simulation
    simulator.Simulate ()

    // output the statistics
    outputResults calls

    ()
