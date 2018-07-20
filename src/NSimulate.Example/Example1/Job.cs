using System;
using System.Collections.Generic;

namespace NSimulate.Example1
{
	/// <summary>
	/// A job to be worked on using workshop machines
	/// </summary>
	public class Job
	{
		public Job ()
		{
			ProcessingTimeRequiredByJobQueue = new Dictionary<Queue<Job>, int>();
		}

		/// <summary>
		/// Gets the processing time required to process this job by each job queue.
		/// </summary>
		/// <value>
		/// The processing time required to process this job by job queue that this job must go through
		/// </value>
		public Dictionary<Queue<Job>, int> ProcessingTimeRequiredByJobQueue { get; }

        /// <summary>
        /// Gets a value indicating whether this <see cref="NSimulate.Example1.Job"/> requires more work.
        /// </summary>
        public bool RequiresMoreWork => ProcessingTimeRequiredByJobQueue.Count > 0;
    }
}

