using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace FSimulate.UnitTest
{
	[TestFixture()]
	public class ProcessFixture
	{
		[Test()]
		public void Constructor_SimulationContextExists_ProcessRegistered()
		{
			using(var context = new SimulationContext()){
				var process = new Process(context);

				Assert.IsNotNull(process.SimulationState);
				Assert.IsTrue(process.SimulationState.IsActive);

				var registeredProcesses = context.GetByType<Process>();
				Assert.IsTrue(registeredProcesses.Contains(process));
			}
		}

		[Test()]
		public void Simulate_EnumerableReturned()
		{
            using (var context = new SimulationContext())
            {
                var process = new Process(context);

                var enumerator = process.Simulate().GetEnumerator();

                Assert.IsNotNull(enumerator);
                Assert.IsFalse(enumerator.MoveNext());
            }
		}
	}
}

