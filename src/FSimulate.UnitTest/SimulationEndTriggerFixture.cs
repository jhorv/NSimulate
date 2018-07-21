using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace FSimulate.UnitTest
{
	[TestFixture()]
	public class SimulationEndTriggerFixture
	{
		[Test()]
		public void Process_EndConditionSpecified_EndConditionMetAtExpectedTime ()
		{
			using(var context = new SimulationContext()){

				var waitInstruction1 = new WaitInstruction(2);
				var waitInstruction2 = new WaitInstruction(4);
				var waitInstruction3 = new WaitInstruction(4);

				var process = new InstructionListTestProcess(context, new List<InstructionBase>(){ waitInstruction1, waitInstruction2, waitInstruction3 });
				var endTrigger = new SimulationEndTrigger(context, ()=>context.TimePeriod >= 5);

				var simulator = new Simulator(context);

				simulator.Simulate();

				Assert.AreEqual(6, context.TimePeriod);

				// the simulation should have ended at th expected time
				Assert.IsTrue(process.SimulationState.IsActive);
			}
		}
	}
}

