using NUnit.Framework;
using System;
using NSimulate;
using System.Linq;

namespace NSimulate.UnitTest
{
	[TestFixture()]
	public class SimulationElementFixture
	{
        #region Test Types

        private class TestElement : SimulationElement
        {
            public TestElement(SimulationContext context)
                : base(context)
            {
            }
        }

        #endregion

        [Test()]
		public void Constructor_SimulationContextExists_ResourceRegistered()
		{
			using(var context = new SimulationContext()){
				var element = new TestElement(context);

				var registeredElements = context.GetByType<TestElement>();
				Assert.IsTrue(registeredElements.Contains(element));
			}
		}
	}

}

