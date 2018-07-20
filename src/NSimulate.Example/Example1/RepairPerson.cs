using System;
using NSimulate;

namespace NSimulate.Example1
{
	/// <summary>
	/// Repair person used to fix machines
	/// </summary>
	public class RepairPerson : Resource
	{
		public RepairPerson(SimulationContext context)
            : base(context)
		{
		}
	}
}

