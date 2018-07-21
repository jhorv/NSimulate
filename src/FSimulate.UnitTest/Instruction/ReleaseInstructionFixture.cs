using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace FSimulate.UnitTest
{
	[TestFixture()]
	public class ReleaseInstructionFixture
	{
		[Test()]
		public void Complete_ResourcesAllocated_ResourcesDeallocated()
		{
            throw new NotImplementedException();
            //using (var context = new SimulationContext()){

            //	int resouceCapacity = 5;
            //	var testResourceSet1 = new TestResource(context, resouceCapacity) { Code = "First", Priority = 10 };
            //	context.Register<TestResource>(testResourceSet1);

            //	var allocateInstruction = new AllocateInstruction<TestResource>(context, resouceCapacity);
            //	allocateInstruction.Complete(context);

            //	Assert.AreEqual(resouceCapacity, testResourceSet1.Allocated);
            //	Assert.AreEqual(testResourceSet1.Allocated, allocateInstruction
            //	   .Allocations
            //	   .First(al=>al.Key == testResourceSet1)
            //	   .Value);

            //	var releaseInstruction = new ReleaseInstruction<TestResource>(context, allocateInstruction);

            //	long? nextTimePeriodCheck;
            //	bool canComplete = releaseInstruction.CanComplete(context, out nextTimePeriodCheck);

            //	Assert.IsTrue(canComplete);
            //	Assert.IsNull(nextTimePeriodCheck);

            //	releaseInstruction.Complete(context);

            //	Assert.AreEqual(0, testResourceSet1.Allocated);
            //	Assert.IsTrue(allocateInstruction.IsReleased);
            //}
        }
	}
}

