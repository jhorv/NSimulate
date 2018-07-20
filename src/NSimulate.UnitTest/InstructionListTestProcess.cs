using NUnit.Framework;
using System;
using NSimulate;
using NSimulate.Instruction;
using System.Linq;
using System.Collections.Generic;

namespace NSimulate.UnitTest
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

		public override System.Collections.Generic.IEnumerator<InstructionBase> Simulate ()
		{
			foreach(InstructionBase instruction in _instructions){
				yield return instruction;
			}
		}
	}
}

