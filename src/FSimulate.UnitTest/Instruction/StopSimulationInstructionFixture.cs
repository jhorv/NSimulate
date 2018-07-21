using System;
using NUnit.Framework;

namespace FSimulate.UnitTest
{
	[TestFixture()]
	public class StopSimulationInstructionFixture
	{
		[Test()]
		public void Complete_ContextPassed_ContextFlaggedForSimulationStop()
		{
			using (var context = new SimulationContext()){

				Assert.IsFalse(context.IsSimulationStopping);
				var instruction = new StopSimulationInstruction();

				long? nextTimePeriodCheck = null;
				bool canComplete = instruction.CanComplete(context, out nextTimePeriodCheck);

				Assert.IsTrue(canComplete);
				instruction.Complete(context);

				Assert.IsTrue(context.IsSimulationStopping);
			}
		}
	}
}

