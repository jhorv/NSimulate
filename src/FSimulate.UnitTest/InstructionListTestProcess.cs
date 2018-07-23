using System;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;

namespace FSimulate.UnitTest
{
	public class InstructionListTestProcess : Process
	{
		private IEnumerable<InstructionBase> _instructions;

		public InstructionListTestProcess(SimulationContext context, IEnumerable<InstructionBase> instructions)
            : base(context)
        {

			_instructions = instructions;
		}

		public InstructionListTestProcess(SimulationContext context, params InstructionBase[] instructions)
            : base(context)
        {
			_instructions = instructions.ToList();
		}

		public override IEnumerable<InstructionBase> Simulate ()
		{
			foreach(InstructionBase instruction in _instructions){
				yield return instruction;
			}
		}
	}
}

