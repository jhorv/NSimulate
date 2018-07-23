using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace FSimulate.UnitTest
{
	[TestFixture()]
	public class WaitAndRaiseNotificationInstructionFixture
	{
		[Test()]
		public void CanComplete_BeforeAndAfterEvent_ReturnsTrueOnlyAfterEvent()
		{
			using (var context = new SimulationContext()){

                var waitInstruction = new WaitNotificationInstruction<object>();
				var process = new InstructionListTestProcess(context, new List<InstructionBase>(){ waitInstruction});

				context.MoveToTimePeriod(0);
				process.SimulationState.InstructionEnumerator = process.Simulate().GetEnumerator();
				process.SimulationState.InstructionEnumerator.MoveNext();

				var testEvent = new TestNotification();
				var raiseInstruction = new RaiseNotificationInstruction<object>(testEvent);

				long? nextTimePeriodCheck;
				bool canComplete = waitInstruction.CanComplete(context, out nextTimePeriodCheck);

				Assert.IsFalse(canComplete);
				Assert.IsNull(nextTimePeriodCheck);

				raiseInstruction.Complete(context);

				canComplete = waitInstruction.CanComplete(context, out nextTimePeriodCheck);
				
				Assert.IsTrue(canComplete);
				Assert.IsNull(nextTimePeriodCheck);
			}
		}

		[Test()]
		public void CanComplete_BeforeAndAfterEventWithCondition_ReturnsTrueOnlyAfterEventMatchingCondition()
		{
			using (var context = new SimulationContext()){

				var waitInstruction = new WaitNotificationInstruction<TestNotification>((e)=>e.Data > 0);
				var process = new InstructionListTestProcess(context, new List<InstructionBase>(){ waitInstruction});
				context.MoveToTimePeriod(0);
				process.SimulationState.InstructionEnumerator = process.Simulate().GetEnumerator();
				process.SimulationState.InstructionEnumerator.MoveNext();

				var testEvent1 = new TestNotification() { Data = 0 };
				var raiseInstruction1 = new RaiseNotificationInstruction<TestNotification>(testEvent1);

				var testEvent2 = new TestNotification() { Data = 1 };
				var raiseInstruction2 = new RaiseNotificationInstruction<TestNotification>(testEvent2);

				long? nextTimePeriodCheck;
				bool canComplete = waitInstruction.CanComplete(context, out nextTimePeriodCheck);
				
				Assert.IsFalse(canComplete);
				Assert.IsNull(nextTimePeriodCheck);

				raiseInstruction1.Complete(context);

				canComplete = waitInstruction.CanComplete(context, out nextTimePeriodCheck);
				
				Assert.IsFalse(canComplete);
				Assert.IsNull(nextTimePeriodCheck);

				raiseInstruction2.Complete(context);

				canComplete = waitInstruction.CanComplete(context, out nextTimePeriodCheck);
				
				Assert.IsTrue(canComplete);
				Assert.IsNull(nextTimePeriodCheck);
				Assert.AreEqual(1,waitInstruction.Notifications.Count);
				Assert.IsTrue(waitInstruction.Notifications.Contains(testEvent2));
			}
		}
	}
}

