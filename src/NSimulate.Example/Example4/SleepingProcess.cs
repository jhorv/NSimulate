using System;
using System.Collections.Generic;
using NSimulate;
using NSimulate.Instruction;

namespace NSimulate.Example4
{
	public class SleepingProcess : Process
	{
        public SleepingProcess(SimulationContext context)
            : base(context)
        {
        }

		public override IEnumerator<InstructionBase> Simulate() 
		{
			Console.WriteLine($"Going to sleep at time period {Context.TimePeriod}");

			// wait till the alarm rings
			yield return new WaitNotificationInstruction<AlarmRingingNotification>();

			Console.WriteLine($"Alarm ringing at time period {Context.TimePeriod}");
			Console.WriteLine($"Going back to sleep at time period {Context.TimePeriod}");

			// go back to sleep and wait till it rings again
			yield return new WaitNotificationInstruction<AlarmRingingNotification>();

			Console.WriteLine($"Alarm ringing again..waking up at time period {Context.TimePeriod}");

			// notify now awake
			var notification = new AwakeNotification();
			yield return RaiseNotificationInstruction.New(notification);
		}
	}
}

