using System;

namespace NSimulate.UnitTest
{
	public class TestResource : Resource
	{
		public TestResource (SimulationContext context, int capacity)
			: base(context, capacity)
		{
		}

		public string Code
		{
			get;
			set;
		}

		public int Priority
		{
			get;
			set;
		}
	}
}

