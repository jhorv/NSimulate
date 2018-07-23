using System;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;

namespace FSimulate.UnitTest
{
	[TestFixture()]
	public class ActivityHostProcessFixture
	{
		[Test()]
		public void Simulate_ActivitySpecified_ActivityFiredAtTheAppropriateTime()
		{
			using(var context = new SimulationContext()){

				var notification = new TestNotification();
				var activity = new TestActivity(new List<InstructionBase>() { new RaiseNotificationInstruction<TestNotification>(notification) });
				long waitTime = 10;

				var process = new ActivityHostProcess(context, activity, waitTime);

				Assert.IsNotNull(process.SimulationState);
				Assert.IsTrue(process.SimulationState.IsActive);

				var registeredProcesses = context.GetByType<Process>();
				Assert.IsTrue(registeredProcesses.Contains(process));

				var enumerator = process.Simulate().GetEnumerator();

				bool couldMove = enumerator.MoveNext();
				Assert.IsTrue(couldMove);

				// first instruction should be the wait instruction
				Assert.IsTrue(enumerator.Current is WaitInstruction);
				Assert.AreEqual(waitTime, ((WaitInstruction)enumerator.Current).NumberOfPeriodsToWait);

				couldMove = enumerator.MoveNext();
				Assert.IsTrue(couldMove);

				Assert.IsTrue(enumerator.Current is RaiseNotificationInstruction<TestNotification>);
				Assert.AreEqual(notification, ((RaiseNotificationInstruction<TestNotification>)enumerator.Current).Notification);

				couldMove = enumerator.MoveNext();
				Assert.IsFalse(couldMove);
			}
		}
	}
}

