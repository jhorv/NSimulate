using System;

namespace NSimulate.Example2
{
    /// <summary>
    /// Characteristics describing attributes of a call
    /// </summary>
    public class CallCharacteristics
    {
        public CallCharacteristics()
        {
        }

        /// <summary>
        /// Gets or sets the time (in terms of number of time periods) before call starts.
        /// </summary>
        public int TimeBeforeCallStarts { get; set; }

        /// <summary>
        /// Gets or sets the call duration (in terms of time periods) at level1.
        /// </summary>
        public int CallDurationAtLevel1 { get; set; }

        /// <summary>
        /// Gets or sets the call duration (in terms of time periods) at level2.
        /// </summary>
        public int CallDurationAtLevel2 { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="NSimulate.Example2.CallCharacteristics"/> call is escalated to level2.
        /// </summary>
        public bool CallIsEscalatedToLevel2 { get; set; }
    }
}
