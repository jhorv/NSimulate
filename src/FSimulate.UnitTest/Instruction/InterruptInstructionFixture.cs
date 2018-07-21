using System;
using NUnit.Framework;

namespace FSimulate.UnitTest
{
	[TestFixture()]
	public class InterruptInstructionFixture
	{
		[Test()]
		public void Complete_ContextPassed_ProcessActivated()
		{
            throw new NotImplementedException();
            //using (var context = new SimulationContext()){
            //	var testProcess = new InstructionListTestProcess(context, new WaitInstruction(10));
            //	var enumerator = testProcess.Simulate();
            //	testProcess.SimulationState.InstructionEnumerator = enumerator;

            //	context.MoveToTimePeriod(0);
            //	Assert.IsTrue(context.ActiveProcesses.Contains(testProcess));
            //	// clear the remaining process queue
            //	context.ProcessesRemainingThisTimePeriod.Clear();

            //	enumerator.MoveNext();

            //	var instruction = new InterruptInstruction(testProcess);

            //	long? nextTimePeriodCheck = null;
            //	bool canComplete = instruction.CanComplete(context, out nextTimePeriodCheck);

            //	Assert.IsTrue(canComplete);
            //	instruction.Complete(context);

            //	Assert.IsTrue(testProcess.SimulationState.IsInterrupted);
            //	// the process should be back in the queue
            //	Assert.IsTrue(context.ProcessesRemainingThisTimePeriod.Contains(testProcess));
            //	Assert.IsTrue(enumerator.Current.IsInterrupted);
            //	Assert.IsFalse(enumerator.Current.IsCompleted);
            //}
        }
	}
}

