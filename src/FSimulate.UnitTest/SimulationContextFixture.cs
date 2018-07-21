using System;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;

namespace FSimulate.UnitTest
{
	[TestFixture()]
	public class SimulationContextFixture
	{
		[Test()]
		public void RegisterAndGet_ObjectRegistered_ObjectCanBeRetrieved ()
		{
            using (var context = new SimulationContext())
            {
                TestResource resource1 = new TestResource(context, 1);
                TestResource resource2 = new TestResource(context, 1);

                context.Register<TestResource>(resource1);
                context.Register<TestResource>(resource2);

                Assert.AreEqual(resource1, context.GetByKey<TestResource>(resource1.Key));
                Assert.AreEqual(resource2, context.GetByKey<TestResource>(resource2.Key));

                var registeredObjects = context.GetByType<TestResource>();
                Assert.IsTrue(registeredObjects.Contains(resource1));
                Assert.IsTrue(registeredObjects.Contains(resource2));
            }
		}

		//[Test()]
		//public void Constructor_IsDefault_ContextSetAsDefaultForProcess ()
		//{
		//	var context = new SimulationContext();
		//	Assert.AreSame(context, SimulationContext.Current);
		//}

		//public void Constructor_IsNotDefault_ContextNotSetAsDefaultForProcess ()
		//{
		//	var context = new SimulationContext(isDefaultContextForProcess: false);
		//	Assert.AreNotSame(context, SimulationContext.Current);
		//}

		[Test()]
		public void MoveToTimePeriod_TimePeriodSpecified_ProcessCollectionsInitialised()
		{
			using (var context = new SimulationContext()){
				var process1 = new Process(context);
				var process2 = new Process(context);
				var process3 = new Process(context);

				var processes = new List<Process>(){
					process1,
					process2,
					process3
				};

				Assert.AreEqual(0, context.TimePeriod);
				context.MoveToTimePeriod(1);
				Assert.AreEqual(1, context.TimePeriod);

				Assert.AreEqual(processes.Count, context.ActiveProcesses.Count);
				foreach(var process in processes){
					Assert.IsTrue(context.ActiveProcesses.Contains(process));
					Assert.IsTrue(context.ProcessesRemainingThisTimePeriod.Contains(process));
				}
			}
		}
	}
}

