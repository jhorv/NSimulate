using System;
using NUnit.Framework;

namespace FSimulate.UnitTest
{
	[TestFixture()]
	public class WaitConditionInstructionFixture
	{
		[Test()]
		public void CanComplete_ConditionNotMet_ReturnsFalse()
		{
			using (var context = new SimulationContext()){
				var instruction = new WaitConditionInstruction(()=>1==2);

				long? nextTimePeriodCheck;
				bool canComplete = instruction.CanComplete(context, out nextTimePeriodCheck);
				
				Assert.IsFalse(canComplete);
				Assert.IsNull(nextTimePeriodCheck);
			}
		}

		[Test()]
		public void CanComplete_ConditionMet_ReturnsTrue()
		{
			using (var context = new SimulationContext()){
				var instruction = new WaitConditionInstruction(()=>1==1);

				long? nextTimePeriodCheck;
				bool canComplete = instruction.CanComplete(context, out nextTimePeriodCheck);
				
				Assert.IsTrue(canComplete);
				Assert.IsNull(nextTimePeriodCheck);
			}
		}
	}
}

