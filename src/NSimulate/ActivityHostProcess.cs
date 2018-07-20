using System.Collections.Generic;
using NSimulate.Instruction;

namespace NSimulate
{
    public class ActivityHostProcess : Process
    {
        public ActivityHostProcess(SimulationContext context, Activity activity, long waitTime)
            : base(context)
        {
            WaitTime = waitTime;
            Activity = activity;
        }

        public long WaitTime { get; private set; }

        public Activity Activity { get; private set; }

        /// <summary>
        /// Simulate the process.
        /// </summary>
        public override IEnumerator<InstructionBase> Simulate()
        {
            // wait for the time the activity is to take place
            yield return new WaitInstruction(WaitTime);

            var timePeriodOfActivityStart = Context.TimePeriod;

            IEnumerator<InstructionBase> enumerator = Activity.Simulate();

            while (enumerator.MoveNext())
            {
                yield return enumerator.Current;
            }
        }
    }
}