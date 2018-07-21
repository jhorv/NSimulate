using System;
using System.Linq;
using NUnit.Framework;

namespace FSimulate.UnitTest
{
	[TestFixture()]
	public class ScheduleActivityInstructionFixture
	{
        class MyActivity : Activity
        {
            public MyActivity()
            {
            }
        }

        [Test()]
		public void Complete_ActivitySpecified_ActivityScheduled()
		{
			using (var context = new SimulationContext()){
			
				long waitTime = 10;
				var activity = new MyActivity();

				var instruction = new ScheduleActivityInstruction(activity, waitTime);

				long? nextTimePeriod;
				bool canComplete = instruction.CanComplete(context, out nextTimePeriod);

				Assert.IsTrue(canComplete);
				Assert.IsNull(nextTimePeriod);

				instruction.Complete(context);

				context.MoveToTimePeriod(0);

				var process = context.ActiveProcesses.FirstOrDefault(p=>p is ActivityHostProcess);
				var activityHost = process as ActivityHostProcess;

				Assert.IsNotNull(process);
				Assert.IsNotNull(activityHost);

				Assert.AreEqual(waitTime, activityHost.WaitTime);
				Assert.AreEqual(activity, activityHost.Activity);
			}
		}
	}
}

