using NUnit.Framework;
using System;
using NSimulate.Instruction;

namespace NSimulate.UnitTest
{
	[TestFixture()]
	public class DeactivateInstructionFixture
	{
		[Test()]
		public void Complete_ContextPassed_ProcessDeActivated()
		{
			using (var context = new SimulationContext()){

				Process testProcess = new Process(context);
				context.MoveToTimePeriod(0);

				Assert.IsTrue(context.ActiveProcesses.Contains(testProcess));

				var instruction = new DeactivateInstruction(testProcess);

				long? nextTimePeriodCheck = null;
				bool canComplete = instruction.CanComplete(context, out nextTimePeriodCheck);

				Assert.IsTrue(canComplete);
				instruction.Complete(context);

				Assert.IsFalse(testProcess.SimulationState.IsActive);
				Assert.IsFalse(context.ActiveProcesses.Contains(testProcess));
			}
		}
	}
}

