using System;
using System.Linq;
using System.Collections.Generic;
using NSimulate.Instruction;

namespace NSimulate
{
	/// <summary>
	/// Base class for all processes
	/// </summary>
	public class Process : SimulationElement
	{
		public static int nextInstanceIndex = 1;

		Priority _priority = Priority.Medium;

		int _instanceIndex = nextInstanceIndex++;

		public Process ()
		{
			SimulationState = new ProcessSimulationState();
		}

		public Process (object key)
			: base(key)
		{
			SimulationState = new ProcessSimulationState();
		}

		public Process (SimulationContext context)
			: base(context)
		{
			SimulationState = new ProcessSimulationState();
		}

		public Process (SimulationContext context, object key)
			: base(context, key)
		{
			SimulationState = new ProcessSimulationState();
		}

		/// <summary>
		/// Gets or sets the simulation state associated with this process
		/// </summary>
		public ProcessSimulationState SimulationState { get; set; }

		public Priority Priority
        {
            get => _priority;
            set => _priority = value;
        }

        public int GetInstanceIndex() => _instanceIndex;

        /// <summary>
        /// Simulate the process.
        /// </summary>
        public virtual IEnumerator<InstructionBase> Simulate()
		{
            yield break;
		}

		/// <summary>
		/// Initialize this instance.
		/// </summary>
		protected override void Initialize()
		{
			base.Initialize ();
		}
	}
}