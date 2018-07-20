using System;
using NSimulate;
using NSimulate.Instruction;

namespace NSimulate.Example4
{
    public class Example
    {
        /// <summary>
        /// Run the example
        /// </summary>
        public static void Run()
        {
            // Make a simulation context
            using (var context = new SimulationContext())
            {
                // instantiate the process responsible for setting alarms
                new AlarmSettingProcess(context);

                new SleepingProcess(context);

                // instantate a new simulator
                var simulator = new Simulator(context);

                // run the simulation
                simulator.Simulate();

                Console.WriteLine("Simulation ended at time period {0}", context.TimePeriod);
            }
        }
    }
}

