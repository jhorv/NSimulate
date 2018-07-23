namespace FSimulate

open System
open System.Collections.Generic
open System.Linq
open System.Reflection
open System.Runtime.InteropServices
open FSharp.Linq

type Priority =
    | VeryHigh = 100
    | High = 200
    | Medium = 300
    | Low = 400
    | VeryLow = 500


[<AbstractClass>]
[<AllowNullLiteral>]
type InstructionBase (priority : Priority) =
    let mutable _completedAtTimePeriod = Unchecked.defaultof<Nullable<int64>>

    new () = InstructionBase(Priority.Medium)

    member val RaisedInTimePeriod = 0L with get, set
    member val IsInterrupted = false with get, set
    member this.CompletedAtTimePeriod with get() = _completedAtTimePeriod
    member this.IsCompleted with get() = _completedAtTimePeriod.HasValue
    
    abstract member Priority : Priority with get, set
    default val Priority = priority with get, set

    abstract member CanComplete : context:ISimulationContext * [<Out>] skipFurtherChecksUntilTimePeriod : Nullable<int64> byref -> bool
    default this.CanComplete (context : ISimulationContext, [<Out>] skipFurtherChecksUntilTimePeriod : Nullable<int64> byref) =
        skipFurtherChecksUntilTimePeriod <- Unchecked.defaultof<Nullable<int64>>
        false

    abstract member Complete : context:ISimulationContext -> unit
    default this.Complete (context:ISimulationContext) =
        // TODO make sure overrides aren't also setting this.CompletedAtTimePeriod
        //this.CompletedAtTimePeriod <- context.TimePeriod |> Nullable<int64>
        _completedAtTimePeriod <- context.TimePeriod |> Nullable<int64>
 
    abstract member Interrupt : context : ISimulationContext -> unit
    default this.Interrupt (context : ISimulationContext) =
        this.IsInterrupted <- true

and [<AllowNullLiteral>] ProcessSimulationState () =
    member val LastInstruction = Unchecked.defaultof<InstructionBase> with get, set
    member val IsActive = true with get, set
    member val IsInterrupted = false with get, set
    member val IsComplete = false with get, set
    member val InstructionEnumerator = Unchecked.defaultof<IEnumerator<InstructionBase>> with get, set

and IProcess =
    abstract member Priority : Priority with get
    abstract member SimulationState : ProcessSimulationState with get
    abstract member Simulate : unit -> IEnumerable<InstructionBase>
    abstract member GetInstanceIndex : unit -> int

and [<AllowNullLiteral>] ISimulationContext =
    abstract member ActiveProcesses : ISet<IProcess> with get
    abstract member IsSimulationStopping : bool with get, set
    abstract member IsSimulationTerminating : bool with get, set
    abstract member TimePeriod : int64 with get
    abstract member ProcessesRemainingThisTimePeriod : Queue<IProcess> with get

    abstract member GetByKey<'T when 'T :> SimulationElement> : key : obj -> 'T
    abstract member GetByType<'T> : unit -> IEnumerable<'T>
    abstract member MoveToTimePeriod : timePeriod : int64 -> unit
    abstract member Register : typeToRegister : Type * objectToRegister : obj -> unit
    abstract member Register<'T when 'T :> SimulationElement> : objectToRegister : 'T -> unit

// TODO - factor out need for "as this"
and [<AbstractClass>] SimulationElement (context : ISimulationContext, key : obj) as this =
    do
        if context <> null then
            context.Register(this.GetType(), this)
    
    new (context:ISimulationContext) =
        SimulationElement(context, Guid.NewGuid())

    member this.Context with get() = context
    member this.Key with get() = key

    abstract member Initialize : unit -> unit
    default this.Initialize () = ()


[<AbstractClass>]
type Activity() =
    abstract member Simulate : unit -> IEnumerable<InstructionBase>
    default this.Simulate () =
        Seq.empty<InstructionBase>



type Process (context : ISimulationContext, key : obj) =
    [<DefaultValue>]
    static val mutable private nextInstanceIndex : int
    static do
        Process.nextInstanceIndex <- 1

    inherit SimulationElement(context, key)

    new (context : ISimulationContext) =
        Process(context, Guid.NewGuid())

    let instanceIndex = System.Threading.Interlocked.Increment(&Process.nextInstanceIndex) - 1
    member val SimulationState = ProcessSimulationState() with get, set
    member val Priority = Priority.Medium with get, set

    member this.GetInstanceIndex() = instanceIndex
    override this.Initialize() = base.Initialize()

    abstract member Simulate : unit -> IEnumerable<InstructionBase>
    default this.Simulate () =
        Seq.empty<InstructionBase>

    interface IProcess with
        member this.Priority with get() = this.Priority
        member this.SimulationState with get() = this.SimulationState
        member this.Simulate () = this.Simulate()
        member this.GetInstanceIndex () = this.GetInstanceIndex()



type WaitInstruction (periods : int64) =
    inherit InstructionBase()
    new (periods : int) = WaitInstruction (int64 periods)
    member this.NumberOfPeriodsToWait with get() = periods
    override this.CanComplete (context : ISimulationContext, [<Out>] skipFurtherChecksUntilTimePeriod : Nullable<int64> byref) =
        let timePeriodToComplete = this.RaisedInTimePeriod + periods
        skipFurtherChecksUntilTimePeriod <- Nullable timePeriodToComplete
        let canComplete = context.TimePeriod >= timePeriodToComplete
        canComplete


type ActivityHostProcess (context : ISimulationContext, activity : Activity, waitTime : int64) =
    inherit Process(context)
    member this.Activity with get() = activity
    member this.WaitTime with get() = waitTime
    override this.Simulate () = seq {
        yield upcast WaitInstruction(waitTime)
        //let timePeriodOfActivityStart = context.TimePeriod
        yield! activity.Simulate()
    }

 
 type Resource (context : ISimulationContext, key : obj, capacity : int) =
    inherit SimulationElement(context, key)

    new (context : ISimulationContext, key : obj) =
        Resource(context, key, 0)

    new (context : ISimulationContext, capacity : int) =
        Resource(context, Guid.NewGuid(), capacity)

    new (context : ISimulationContext) =
        Resource(context, 0)

    member val Allocated = 0 with get, set
    member val Capacity = capacity with get, set


 
 type ActivateInstruction (p : Process) =
    inherit InstructionBase()

    override this.CanComplete(context:ISimulationContext, [<Out>] skipFurtherChecksUntilTimePeriod : Nullable<int64> byref) =
        skipFurtherChecksUntilTimePeriod <- Unchecked.defaultof<Nullable<int64>>
        true
    
    override this.Complete(context:ISimulationContext) =
        base.Complete(context)

        if p.SimulationState.IsActive = false then
            p.SimulationState.IsActive <- true

        if context.ActiveProcesses.Add p then
            // ensure the process just activated will be processed by the simulator again in the current time period
            if context.ProcessesRemainingThisTimePeriod.Contains p = false then
                context.ProcessesRemainingThisTimePeriod.Enqueue p


/// Instruction used to allocate resources
[<AllowNullLiteral>]
type AllocateInstruction<'R when 'R :> Resource>
    (number : int
    , resourceMatchFunction : Func<'R, bool>
    , resourcePriorityFunction : Func<'R, int>)
    =
    
    inherit InstructionBase(Priority.Low)

    let _resourceMatchFunction =
        if resourceMatchFunction <> null then
            resourceMatchFunction
        else
            (Func<'R, bool>(fun o -> true))

    let _resourcePriorityFunction = if resourcePriorityFunction <> null then resourcePriorityFunction else Unchecked.defaultof<Func<'R, int>>
    let mutable _allocations = Unchecked.defaultof<List<KeyValuePair<'R, int>>>
    let mutable _numberRequested = number
    let mutable _isAllocated = false
    let mutable _isReleased = false

    let getSortedResources (c : ISimulationContext) =
        if _resourcePriorityFunction = null then
            c.GetByType<'R>()
                .Where(_resourceMatchFunction)
        else
            c.GetByType<'R>()
                .Where(_resourceMatchFunction)
                .OrderBy(_resourcePriorityFunction)
                :> IEnumerable<'R>

    new (number : int, resourceMatchFunction : Func<'R, bool>) =
        AllocateInstruction(number, resourceMatchFunction, null)

    new (number : int) =
        AllocateInstruction(number, null, null)
    
    member this.Allocations with get() = _allocations
    member this.NumberRequested with get() = _numberRequested
    member this.IsAllocated with get() = _isAllocated
    member this.IsReleased with get() = _isReleased

    override this.CanComplete (context : ISimulationContext, [<Out>] skipFurtherChecksUntilTimePeriod : Nullable<int64> byref) =
        skipFurtherChecksUntilTimePeriod <- Unchecked.defaultof<Nullable<int64>>

        let resources = getSortedResources context

        // TODO: refactor C# translation (currently inefficient because of no F# break statement)
        let mutable enoughAvailable = false
        let mutable available = 0
        for resource in resources do
            if enoughAvailable = false then
                let stillAvailable = resource.Capacity - resource.Allocated
                if stillAvailable > 0 then
                    available <- available + stillAvailable
                if available >= _numberRequested then
                    enoughAvailable <- true

        enoughAvailable

    override this.Complete (context : ISimulationContext) =
        base.Complete(context)

        let resources = getSortedResources context

        _allocations <- List<KeyValuePair<'R, int>> ()

        // TODO: refactor C# translation (currently inefficient because of no F# break statement)
        let mutable allocated = 0
        for resource in resources do
            if allocated <> _numberRequested then
                let available = resource.Capacity - resource.Allocated
                if available > 0 then
                    let amountToAllocate = Math.Min(available, _numberRequested - allocated)
                    _allocations.Add(KeyValuePair<'R, int>(resource, amountToAllocate))
                    resource.Allocated <- resource.Allocated + amountToAllocate
                    allocated <- allocated + amountToAllocate

        _isAllocated <- true
        _isReleased <- false
        
        // This is done by base.Complete
        //this.CompletedAtTimePeriod <- Nullable<int64>(context.TimePeriod)

    member this.Release () =
        if _isAllocated && _isReleased = false then
            for allocation in _allocations do
                allocation.Key.Allocated <- allocation.Key.Allocated - allocation.Value

            _isAllocated <- false
            _isReleased <- true



/// A Composite instruction which groups a set of other instructions.
type CompositeInstruction (instructions : IEnumerable<InstructionBase>) =
    inherit InstructionBase()
    let instructions = instructions.ToList()

    new ([<ParamArray>] instructions : InstructionBase[]) =
        CompositeInstruction(instructions)

    override this.CanComplete (context : ISimulationContext, [<Out>] skipFurtherChecksUntilTimePeriod : Nullable<int64> byref) =
        skipFurtherChecksUntilTimePeriod <- Unchecked.defaultof<Nullable<int64>>

        let mutable canComplete = true
        let mutable nullCheckTimePeriodEncountered = false

        // iterate through all contained instructions, check whether they can all complete
        // the value returned in skipFurtherChecksUntilTimePeriod is the lowest value of any instrution, and null if any return null for this value
        for instruction in instructions do
            let mutable nextCheckTimePeriod = Unchecked.defaultof<Nullable<int64>>
            let thisInstructionCanComplete = instruction.CanComplete(context, &nextCheckTimePeriod)
            if not thisInstructionCanComplete then
                canComplete <- false
                if not nextCheckTimePeriod.HasValue then
                    nullCheckTimePeriodEncountered <- true
                    nextCheckTimePeriod <- Unchecked.defaultof<Nullable<int64>>
                elif not nullCheckTimePeriodEncountered &&
                     (skipFurtherChecksUntilTimePeriod.HasValue = false || skipFurtherChecksUntilTimePeriod.Value > nextCheckTimePeriod.Value) then
                    skipFurtherChecksUntilTimePeriod <- nextCheckTimePeriod

        if canComplete then
            skipFurtherChecksUntilTimePeriod <- Unchecked.defaultof<Nullable<int64>>

        canComplete

    override this.Complete (context : ISimulationContext) =
        for instruction in instructions do
            instruction.Complete context

        base.Complete context


/// An instruction used to deactivate a process
type DeactivateInstruction (p : IProcess) =
    inherit InstructionBase()
    
    override this.CanComplete (context : ISimulationContext, [<Out>] skipFurtherChecksUntilTimePeriod : Nullable<int64> byref) =
        skipFurtherChecksUntilTimePeriod <- Unchecked.defaultof<Nullable<int64>>
        true
    
    override this.Complete (context : ISimulationContext) =
        base.Complete context

        // de-activate the process
        if p.SimulationState.IsActive then
            p.SimulationState.IsActive <- false

        context.ActiveProcesses.Remove(p) |> ignore


/// An instruction used to interrupt another process.
type InterruptInstruction (p : IProcess) =
    inherit InstructionBase()
    
    override this.CanComplete (context : ISimulationContext, [<Out>] skipFurtherChecksUntilTimePeriod : Nullable<int64> byref) =
        skipFurtherChecksUntilTimePeriod <- Unchecked.defaultof<Nullable<int64>>
        true
       
    override this.Complete (context : ISimulationContext) =
        base.Complete context

        // interrupt the process
        p.SimulationState.IsInterrupted <- true
        if p.SimulationState.InstructionEnumerator <> null && p.SimulationState.InstructionEnumerator.Current <> null then
            p.SimulationState.InstructionEnumerator.Current.Interrupt context

        if p.SimulationState.IsActive then
            if context.ProcessesRemainingThisTimePeriod.Contains p = false then
                context.ProcessesRemainingThisTimePeriod.Enqueue p


/// An instruction used to pass control from a process back to the simulator without performing any other action.
type PassInstruction() =
    inherit InstructionBase()
    let mutable hasPassed = false
    override this.CanComplete (context : ISimulationContext, [<Out>] skipFurtherChecksUntilTimePeriod : Nullable<int64> byref) =
        skipFurtherChecksUntilTimePeriod <- Unchecked.defaultof<Nullable<int64>>
        let canComplete = hasPassed
        if not hasPassed then hasPassed <- true
        canComplete



/// An instruction that causes a process to wait until an event is fired
type WaitNotificationInstruction<'T> (matchingCondition : Func<'T, bool>) =
    inherit InstructionBase(Priority.Low)

    let notifications = List<'T>()

    new () = WaitNotificationInstruction(null)

    member this.MatchingCondition with get() = matchingCondition
    member this.Notifications with get() = notifications

    // TODO move logic to this type that adds items to Notifications
    override this.CanComplete (context : ISimulationContext, [<Out>] skipFurtherChecksUntilTimePeriod : Nullable<int64> byref) =
        skipFurtherChecksUntilTimePeriod <- Unchecked.defaultof<Nullable<int64>>
        notifications.Count > 0


/// An instruction used to raise an event
type RaiseNotificationInstruction<'T> (notificationToRaise : 'T) =
    inherit InstructionBase()
    member this.Notification with get() = notificationToRaise
    override this.CanComplete (context : ISimulationContext, [<Out>] skipFurtherChecksUntilTimePeriod : Nullable<int64> byref) =
        skipFurtherChecksUntilTimePeriod <- Unchecked.defaultof<Nullable<int64>>
        true
    override this.Complete (context : ISimulationContext) =
        base.Complete context

        // Add the event to the wait instructions of all processes currently waiting
        if context.ActiveProcesses <> null then
            for p in context.ActiveProcesses do
                if p.SimulationState <> null
                    && p.SimulationState.InstructionEnumerator <> null
                    && p.SimulationState.InstructionEnumerator.Current <> null
                    && p.SimulationState.InstructionEnumerator.Current :? WaitNotificationInstruction<'T> then
                    let waitEventInstruction = p.SimulationState.InstructionEnumerator.Current :?> WaitNotificationInstruction<'T>
                    if waitEventInstruction.MatchingCondition = null || waitEventInstruction.MatchingCondition.Invoke(notificationToRaise) then
                        waitEventInstruction.Notifications.Add(notificationToRaise)


/// An instruction used to release allocated resources
type ReleaseInstruction<'T when 'T :> Resource> (allocateInstruction : AllocateInstruction<'T>) =
    inherit InstructionBase()
    
    override this.CanComplete (context : ISimulationContext, [<Out>] skipFurtherChecksUntilTimePeriod : Nullable<int64> byref) =
        skipFurtherChecksUntilTimePeriod <- Unchecked.defaultof<Nullable<int64>>
        true

    override this.Complete (context : ISimulationContext) =
        base.Complete context

        if allocateInstruction <> null && allocateInstruction.IsAllocated && not allocateInstruction.IsReleased then
            allocateInstruction.Release ()



type ScheduleActivityInstruction (activity : Activity, waitTime : int64) =
    inherit InstructionBase()
    member this.Activity with get() = activity
    member this.WaitTime with get() = waitTime
    
    override this.CanComplete (context : ISimulationContext, [<Out>] skipFurtherChecksUntilTimePeriod : Nullable<int64> byref) =
        skipFurtherChecksUntilTimePeriod <- Unchecked.defaultof<Nullable<int64>>
        true
    
    override this.Complete (context : ISimulationContext) =
        base.Complete context
        
        let p = ActivityHostProcess(context, activity, waitTime)
        context.Register(p)

        if context.ActiveProcesses <> null then
            // add it to the active process list
            context.ActiveProcesses.Add(p) |> ignore
            context.ProcessesRemainingThisTimePeriod.Enqueue(p)


/// An instruction used to stop the simulation.
/// <remarks>This instruction, when completed, marks the simulation as stopping.  Individual processes are responsible for responding to this flag.</remarks>
type StopSimulationInstruction() =
    inherit InstructionBase()

    override this.CanComplete (context : ISimulationContext, [<Out>] skipFurtherChecksUntilTimePeriod : Nullable<int64> byref) =
        skipFurtherChecksUntilTimePeriod <- Unchecked.defaultof<Nullable<int64>>
        true

    override this.Complete (context : ISimulationContext) =
        base.Complete context
        context.IsSimulationStopping <- true


/// An instruction used to termine the simulation.
/// <remarks>This instruction, when completed, marks the simulation as terminating.  The simulator terminates the simulation without relying on individual processes to stop.</remarks>
type TerminateSimulationInstruction() =
    inherit InstructionBase()
    override this.CanComplete (context : ISimulationContext, [<Out>] skipFurtherChecksUntilTimePeriod : Nullable<int64> byref) =
        skipFurtherChecksUntilTimePeriod <- Unchecked.defaultof<Nullable<int64>>
        true
    override this.Complete (context : ISimulationContext) =
        base.Complete context
        context.IsSimulationTerminating <- true


/// Instruction used to wait until a condition is met
type WaitConditionInstruction (condition : Func<bool>) =
    inherit InstructionBase()
    member this.Condition with get() = condition
    override this.CanComplete (context : ISimulationContext, [<Out>] skipFurtherChecksUntilTimePeriod : Nullable<int64> byref) =
        skipFurtherChecksUntilTimePeriod <- Unchecked.defaultof<Nullable<int64>>
        condition.Invoke ()



/// A process that acts as a trigger for ending the simulation
type SimulationEndTrigger (context:ISimulationContext, condition:Func<bool>) =
    inherit Process(context)

    member this.Condition with get() = condition

    override this.Simulate () =
        seq {
            yield WaitConditionInstruction(condition) :> InstructionBase
            yield TerminateSimulationInstruction() :> InstructionBase
        }
        

/// A Simulator used to orchestrate the simulation of processes in the overall simulation context
type Simulator (context : ISimulationContext) =

    let simulateProcessAtTimePeriod (p : IProcess, nextTimePeriod : Nullable<int64> byref) =
        let mutable shouldMoveNext = true

        while shouldMoveNext && context.IsSimulationTerminating = false do
            let currentInstruction = p.SimulationState.InstructionEnumerator.Current
            
            let mutable didComplete = false
            
            if currentInstruction <> null then
            
                let mutable nextTimePeriodCheck = Unchecked.defaultof<Nullable<int64>>
    
                if p.SimulationState.IsInterrupted then
                    currentInstruction.Interrupt(context)
                elif currentInstruction.IsInterrupted || currentInstruction.IsCompleted then
                    // no further processing of the instruction is needed
                    ()
                elif currentInstruction.CanComplete(context, &nextTimePeriodCheck) then
                    currentInstruction.Complete(context)
                    didComplete <- true
                else
                    shouldMoveNext <- false

                if not didComplete then
                    if nextTimePeriodCheck.HasValue && (nextTimePeriod.HasValue = false || nextTimePeriodCheck.Value < nextTimePeriod.Value) then
                        nextTimePeriod <- nextTimePeriodCheck

            if shouldMoveNext then
                let couldMoveNext = p.SimulationState.InstructionEnumerator.MoveNext()
                if couldMoveNext then
                    p.SimulationState.InstructionEnumerator.Current.RaisedInTimePeriod <- context.TimePeriod
                else
                    shouldMoveNext <- false
                    p.SimulationState.IsComplete <- true
                    context.ActiveProcesses.Remove(p) |> ignore

        if p.SimulationState.IsInterrupted then
            // ensure the process is not in the interrupted state
            p.SimulationState.IsInterrupted <- false
    
    member this.Simulate () =
        context.MoveToTimePeriod 0L

        let mutable complete = false
        let mutable nextTimePeriod = Unchecked.defaultof<Nullable<int64>>

        while not complete do
            while context.ProcessesRemainingThisTimePeriod.Count > 0 && context.IsSimulationTerminating = false do
                let p = context.ProcessesRemainingThisTimePeriod.Dequeue()

                if p.SimulationState.IsActive then
                    if isNull p.SimulationState.InstructionEnumerator then
                        p.SimulationState.InstructionEnumerator <- p.Simulate().GetEnumerator()
                    
                    simulateProcessAtTimePeriod (p, &nextTimePeriod)

            if nextTimePeriod.HasValue && context.IsSimulationTerminating = false then
                // Move to the next time period
                context.MoveToTimePeriod nextTimePeriod.Value
                nextTimePeriod <- Unchecked.defaultof<Nullable<int64>>
            else
                // simulation has completed
                complete <- true


/// Context containing state information for a simulation
type SimulationContext () =
    let _registeredElements = new Dictionary<Type, Dictionary<obj, SimulationElement>>()
    let mutable _timePeriod = 0L
    let mutable _isSimulationTerminating = false
    let mutable _isSimulationStopping = false
    let mutable _activeProcesses = Unchecked.defaultof<HashSet<IProcess>>
    let mutable _processesRemainingThisTimePeriod = Unchecked.defaultof<Queue<IProcess>>
    let mutable _processedProcesses = Unchecked.defaultof<HashSet<IProcess>>

    let isTypeEqualOrSubclass (typeToCheck : Type, typeToCompare : Type) =
        let mutable isMatch = false
        let mutable currentType = typeToCheck
        while not isMatch && currentType <> null do
            if currentType = typeToCompare then
                isMatch <- true
            else
                currentType <- currentType.GetTypeInfo().BaseType;
        isMatch

    member this.TimePeriod with get() = _timePeriod
    member this.IsSimulationTerminating
        with get() = _isSimulationTerminating
        and set(value) = _isSimulationTerminating <- value
    member this.IsSimulationStopping
        with get() = _isSimulationStopping
        and set(value) = _isSimulationStopping <- value
    member this.ActiveProcesses with get() = _activeProcesses :> ISet<IProcess>
    member this.ProcessesRemainingThisTimePeriod with get() = _processesRemainingThisTimePeriod
    member this.ProcessedProcesses with get() = _processedProcesses :> ISet<IProcess>

    member this.GetByKey<'T> (key : obj) =
        let mutable objectToRetrieve = Unchecked.defaultof<obj>
        for entry in _registeredElements do
            if objectToRetrieve = null && isTypeEqualOrSubclass(entry.Key, typedefof<'T>) then
                match entry.Value.TryGetValue(key) with
                | true, x -> objectToRetrieve <- x
                | _ -> ()
        objectToRetrieve :?> 'T

    // TODO - make this better/more efficient
    member this.GetByType<'T> () : IEnumerable<'T> =
        let enumerableToReturn = List<'T>()
        for entry in _registeredElements do
            if isTypeEqualOrSubclass(entry.Key, typedefof<'T>) then
                enumerableToReturn.AddRange(entry.Value.Values.Cast<'T>())
        enumerableToReturn :> IEnumerable<'T>

    member this.MoveToTimePeriod (timePeriod : int64) =
        _timePeriod <- timePeriod

        if isNull _activeProcesses then
            _activeProcesses <- HashSet<IProcess>()
            let processes = this.GetByType<Process>()
            for p in processes do
                if isNull p.SimulationState then
                    p.SimulationState <- ProcessSimulationState()
                if p.SimulationState.IsActive then
                    _activeProcesses.Add(p) |> ignore

        let tryGetProcessStatePriority (p : IProcess) =
            if p.SimulationState <> null && p.SimulationState.InstructionEnumerator <> null && p.SimulationState.InstructionEnumerator.Current <> null then
                p.SimulationState.InstructionEnumerator.Current.Priority
            else
                Priority.Medium

        let tryGetProcessTimePeriod (p : IProcess) =
            if p.SimulationState <> null && p.SimulationState.InstructionEnumerator <> null && p.SimulationState.InstructionEnumerator.Current <> null then
                p.SimulationState.InstructionEnumerator.Current.RaisedInTimePeriod
            else
                timePeriod

        // order processes by process priority, instruction priority, insruction raise time, and then process instance
        let processesInPriorityOrder =
            _activeProcesses
                .OrderBy(fun p -> p.Priority)
                .ThenBy(Func<IProcess, Priority>(tryGetProcessStatePriority))
                .ThenBy(Func<IProcess, int64>(tryGetProcessTimePeriod))
                .ThenBy(Func<IProcess, int>(fun p -> p.GetInstanceIndex()))

        _processesRemainingThisTimePeriod <- new Queue<IProcess>(_activeProcesses.Count)
        for p in processesInPriorityOrder do
            _processesRemainingThisTimePeriod.Enqueue(p)
        _processedProcesses <- HashSet<IProcess>()

    member this.Register (typeToRegister : Type, objectToRegister : obj) =
        if isTypeEqualOrSubclass(typeToRegister, typedefof<SimulationElement>) = false then
            raise <| ArgumentException("typeToRegister")

        let element = objectToRegister :?> SimulationElement

        match _registeredElements.TryGetValue(typeToRegister) with
        | true, elementsByKey -> elementsByKey.[element.Key] <- element
        | _ ->
            let elementsByKey = Dictionary<obj, SimulationElement>()
            elementsByKey.Add(element.Key, element)
            _registeredElements.[typeToRegister] <- elementsByKey

    member this.Register<'T when 'T :> SimulationElement> (objectToRegister : 'T) =
        this.Register(typedefof<'T>,  objectToRegister)

    interface IDisposable with
        member this.Dispose () = ()

    interface ISimulationContext with
        member this.ActiveProcesses with get() = this.ActiveProcesses
        member this.IsSimulationStopping
            with get() = this.IsSimulationStopping
            and set(value) = this.IsSimulationStopping <- value
        member this.IsSimulationTerminating
            with get() = this.IsSimulationTerminating
            and set(value) = this.IsSimulationTerminating <- value
        member this.TimePeriod with get() = this.TimePeriod
        member this.ProcessesRemainingThisTimePeriod with get() = this.ProcessesRemainingThisTimePeriod
        member this.GetByKey<'T when 'T :> SimulationElement> (key : obj) = this.GetByKey<'T>(key)
        member this.GetByType<'T> () = this.GetByType<'T>()
        member this.MoveToTimePeriod (timePeriod : int64) = this.MoveToTimePeriod(timePeriod)
        member this.Register (typeToRegister : Type, objectToRegister : obj) = this.Register(typeToRegister, objectToRegister)
        member this.Register<'T when 'T :> SimulationElement> (objectToRegister : 'T) = this.Register<'T>(objectToRegister)
