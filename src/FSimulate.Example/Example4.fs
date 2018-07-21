module Example4
open System
open FSimulate

type AlarmRingingNotification() = class end
type AwakeNotification() = class end

type AlarmActivity () =
    inherit Activity()
    override this.Simulate () =
        (seq {
            yield RaiseNotificationInstruction<AlarmRingingNotification>(AlarmRingingNotification()) :> InstructionBase
        }).GetEnumerator()

type AlarmSettingProcess (context : ISimulationContext) =
    inherit Process(context)
    override this.Simulate () =
        (seq {
            // set the alarm activity to occur 8 time periods from now
            yield ScheduleActivityInstruction(AlarmActivity(), 8L) :> InstructionBase

            // and another alarm activity to occur 9 time periods from now
            yield ScheduleActivityInstruction(AlarmActivity(), 9L) :> InstructionBase
        }).GetEnumerator()

type SleepingProcess (context : ISimulationContext) =
    inherit Process(context)
    override this.Simulate() =
        (seq {
            //Console.WriteLine("Going to sleep at time period {0}", this.Context.TimePeriod)
            Console.WriteLine("Going to sleep at time period {0}", context.TimePeriod)
            
            // wait till the alarm rings
            yield WaitNotificationInstruction<AlarmRingingNotification>() :> InstructionBase

            //Console.WriteLine("Alarm ringing at time period {0}", this.Context.TimePeriod)
            Console.WriteLine("Alarm ringing at time period {0}", context.TimePeriod)
            //Console.WriteLine("Going back to sleep at time period {0}", this.Context.TimePeriod)
            Console.WriteLine("Going back to sleep at time period {0}", context.TimePeriod)

            // go back to sleep and wait till it rings again
            yield new WaitNotificationInstruction<AlarmRingingNotification>() :> InstructionBase

            //Console.WriteLine("Alarm ringing again..waking up at time period {0}", this.Context.TimePeriod)
            Console.WriteLine("Alarm ringing again..waking up at time period {0}", context.TimePeriod)

            // notify now awake
            let notification = new AwakeNotification();
            yield RaiseNotificationInstruction(notification) :> InstructionBase

        }).GetEnumerator()


let run () =
    use context = new SimulationContext()

    // instantiate the process responsible for setting alarms
    AlarmSettingProcess(context) |> ignore

    SleepingProcess(context) |> ignore

    // instantiate a new simulator
    let simulator = new Simulator(context)

    // run the simulation
    simulator.Simulate ()

    Console.WriteLine("Simulation ended at time period {0}", context.TimePeriod)

    ()


