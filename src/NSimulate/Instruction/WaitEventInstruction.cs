using System;
using System.Linq;
using System.Collections.Generic;

namespace NSimulate.Instruction
{
	/// <summary>
	/// An instruction that causes a process to wait until an event is fired
	/// </summary>
	public class WaitEventInstruction<TEvent> : InstructionBase
	{
		public WaitEventInstruction (Func<TEvent, bool> matchingCondition = null)
		{
			MatchingCondition = matchingCondition;
			Events = new List<TEvent>();
		}

		public List<TEvent> Events{
			get;
			private set;
		}

		public Func<TEvent, bool> MatchingCondition {
			get;
			private set;
		}

		/// <summary>
		/// Determines whether this instruction can complete in the current time period
		/// </summary>
		/// <returns>
		/// <c>true</c> if this instance can complete.
		/// </returns>
		/// <param name='context'>
		/// Context providing state information for the current simulation
		/// </param>
		/// <param name='skipFurtherChecksUntilTimePeriod'>
		/// Output parameter used to specify a time period at which this instruction should be checked again.  This should be left null if it is not possible to determine when this instruction can complete.
		/// </param>
		public override  bool CanComplete(SimulationContext context, out long? skipFurtherChecksUntilTimePeriod){
			skipFurtherChecksUntilTimePeriod = null;

			bool canComplete = false;

			if (Events != null && Events.Count > 0){
				canComplete = true;
			}

			return canComplete;
		}
	}
}

