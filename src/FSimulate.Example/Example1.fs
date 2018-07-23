module Example1
open System
open System.Collections.Generic
open System.Linq
open FSimulate

type Job () =
    member val ProcessingTimeRequiredByJobQueue = new Dictionary<Queue<Job>, int>() with get
    member this.RequiresMoreWork with get() = this.ProcessingTimeRequiredByJobQueue.Count > 0

type RepairPerson (context : ISimulationContext) =
    inherit Resource(context)
    
type Machine
    (context : ISimulationContext
    , jobQueue : Queue<Job>
    , reliabilityPercentage : float
    , repairTimeRequired : int
    , unprocessedJobsList : List<Job>
    , random : Random) =

    inherit Process(context)

    let checkForRandomBreakdown () =
        let randomPercentage = random.NextDouble() * 100.0
        randomPercentage > reliabilityPercentage

    member val BreakdownCount = 0 with get, set
    member val ProcessedCount = 0 with get, set

    override this.Simulate() =
        seq {
            // while the simulation is running
            while true do

                // check if the queue for this machine is empty
                if jobQueue.Count = 0 then
                    // if it is, wait until there is something in the queue
                    yield upcast WaitConditionInstruction (Func<bool>(fun () -> jobQueue.Count > 0))
                else
                    // take a job from the queue
                    let jobToProcess = jobQueue.Dequeue ()

                    // simulate processing the job
                    // which takes time
                    yield upcast WaitInstruction(jobToProcess.ProcessingTimeRequiredByJobQueue.[jobQueue])

                    // use the reliability indicator to determine if the machine is broken down
                    if checkForRandomBreakdown () then
                        this.BreakdownCount <- this.BreakdownCount + 1
                        // the machine has broken down
                        // add the job it was processing back to the queue
                        jobQueue.Enqueue jobToProcess

                        // obtain a repair person
                        let allocateRepairPerson = AllocateInstruction<RepairPerson> 1
                        yield upcast allocateRepairPerson
                        
                        // and wait for the machine to be fixed
                        yield upcast WaitInstruction repairTimeRequired

                        // then release the repair person resource
                        yield upcast ReleaseInstruction<RepairPerson> allocateRepairPerson

                    else
                        this.ProcessedCount <- this.ProcessedCount + 1
                        // recorde the fact that the job has been processed by this machine type
                        jobToProcess.ProcessingTimeRequiredByJobQueue.Remove(jobQueue) |> ignore

                        // if the job still rqeuires other processing
                        if jobToProcess.RequiresMoreWork then
                            // add it to the next queue
                            jobToProcess.ProcessingTimeRequiredByJobQueue.Keys.First().Enqueue jobToProcess
                        else
                            // otherwise remove it from the all unprocessed jobs list
                            unprocessedJobsList.Remove jobToProcess |> ignore
        }

let createModel (context : ISimulationContext, numberOfJobs : int, randomSeed : int) =
    let rand = Random randomSeed

    let unprocessedJobsList = List<Job>()

    // Craete job queues of various work types
    let workTypeAJobQueue = Queue<Job>()
    let workTypeBJobQueue = Queue<Job>()
    let workTypeCJobQueue = Queue<Job>()
    let workQueues = List<Queue<Job>> [ workTypeAJobQueue; workTypeBJobQueue; workTypeCJobQueue ]

    let random = Random randomSeed
    let machine1 = Machine(context, workTypeAJobQueue, 95.0, 15, unprocessedJobsList, random)
    let machine2 = Machine(context, workTypeAJobQueue, 85.0, 22, unprocessedJobsList, random)
    let machine3 = Machine(context, workTypeBJobQueue, 99.0, 15, unprocessedJobsList, random)
    let machine4 = Machine(context, workTypeBJobQueue, 96.0, 17, unprocessedJobsList, random)
    let machine5 = Machine(context, workTypeCJobQueue, 98.0, 20, unprocessedJobsList, random)

    let machines = List<Machine> [ machine1; machine2; machine3; machine4; machine5 ]

    // create the jobs
    for i in 0 .. (numberOfJobs - 1) do
        let newJob = new Job()

        newJob.ProcessingTimeRequiredByJobQueue.[workTypeAJobQueue] <- 5 + rand.Next(5)
        newJob.ProcessingTimeRequiredByJobQueue.[workTypeBJobQueue] <- 5 + rand.Next(5)
        newJob.ProcessingTimeRequiredByJobQueue.[workTypeCJobQueue] <- 5 + rand.Next(5)

        let index = rand.Next workQueues.Count
        // enqueue the job in one of the work queues
        workQueues.[index].Enqueue newJob

        // and add it to the unprocessed job list
        unprocessedJobsList.Add newJob

    // add a repair person
    let repairPerson = RepairPerson(context)
    repairPerson.Capacity <- 1

    // add the end condition
    new SimulationEndTrigger(context, fun () -> unprocessedJobsList.Count = 0) |> ignore

    machines


let run () =
    use context = new SimulationContext()
    
    let randomSeed = 7

    // initialize the mdoel
    let machines = createModel (context, 500, randomSeed)

    // instantiate a new simulator
    let simulator = Simulator context

    // run the simulator
    simulator.Simulate ()

    printfn "Jobs processed in %i minutes" context.TimePeriod

    let mutable index = 1
    for machine in machines do
        Console.WriteLine("Machine {0} processed {1} jobs and had {2} breakdowns.", index, machine.ProcessedCount, machine.BreakdownCount)
        index <- index + 1



    ()