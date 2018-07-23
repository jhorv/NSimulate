using System;
using System.Collections.Generic;

namespace FSimulate.UnitTest
{
	public class TestActivity : Activity
	{
		public TestActivity (List<InstructionBase> instructions)
		{
			Instructions = instructions;
		}

		public List<InstructionBase> Instructions{
			get;
			private set;
		}

        public override IEnumerable<InstructionBase> Simulate()
        {
            foreach (var instruction in Instructions)
            {
                yield return instruction;
            }
        }
    }
}

