using System;
using System.Linq;
using System.Collections.Generic;
using NSimulate.Instruction;

namespace NSimulate
{
	/// <summary>
	/// The simulation state for a process
	/// </summary>
	public class ProcessSimulationState
	{
		public ProcessSimulationState()
		{
			IsActive = true;
		}

		/// <summary>
		/// The last instruction issued by the process
		/// </summary>
		public InstructionBase LastInstruction { get; set; }

        /// <summary>
        /// Indicates whether the process is active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Indicates whether the process has been interrupted
        /// </summary>
        public bool IsInterrupted { get; set; }

		/// <summary>
		/// Indicates whether the process has been completed.
		/// </summary>
		public bool IsComplete { get; set; }

		/// <summary>
		/// The enumerator used to iterate through instructions issued by this process.
		/// </summary>
		public IEnumerator<InstructionBase> InstructionEnumerator { get; set; }
	}
}

