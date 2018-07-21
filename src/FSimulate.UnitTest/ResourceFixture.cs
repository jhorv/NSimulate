using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace FSimulate.UnitTest
{
	[TestFixture()]
	public class ResourceFixture
	{
		[Test()]
		public void Constructor_SimulationContextExists_ResourceRegistered()
		{
			using(var context = new SimulationContext()){
				int capacity = 10;
				var resource = new Resource(context, capacity);

				Assert.AreEqual(0, resource.Allocated);
				Assert.AreEqual(capacity, resource.Capacity);

				var registeredResources = context.GetByType<Resource>();
				Assert.IsTrue(registeredResources.Contains(resource));
			}
		}
	}
}

