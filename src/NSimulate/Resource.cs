using System;

namespace NSimulate
{
    /// <summary>
    /// A Resource.
    /// </summary>
    public class Resource : SimulationElement
    {
        public Resource()
        {
        }

        public Resource(int capacity)
        {
            Capacity = capacity;
        }

        public Resource(object key, int capacity)
            : base(key)
        {
            Capacity = capacity;
        }

        public Resource(SimulationContext context, int capacity)
            : base(context)
        {
            Capacity = capacity;
        }

        public Resource(SimulationContext context, object key, int capacity)
            : base(context, key)
        {
            Capacity = capacity;
        }

        /// <summary>
        /// Gets or sets the number (quantity) of this resource allocated
        /// </summary>
        public int Allocated { get; set; }

        /// <summary>
        /// Gets the capacity in terms of number / quantity that can be allocated.
        /// </summary>
        public int Capacity { get; set; }
    }
}

