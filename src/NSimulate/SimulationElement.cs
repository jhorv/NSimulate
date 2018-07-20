using System;

namespace NSimulate
{
    /// <summary>
    /// Base class for all simulation elements
    /// </summary>
    public abstract class SimulationElement
    {
        //protected SimulationElement()
        //    : this(SimulationContext.Current, Guid.NewGuid())
        //{
        //}

        //protected SimulationElement(object key)
        //    : this(SimulationContext.Current, key)
        //{
        //}

        protected SimulationElement(SimulationContext context)
            : this(context, Guid.NewGuid())
        {
        }

        protected SimulationElement(SimulationContext context, object key)
        {
            Key = key;
            if (context != null)
            {
                context.Register(GetType(), this);
            }

            Context = context;
        }

        /// <summary>
        /// Gets the key that identifies this element
        /// </summary>
        public object Key { get; private set; }

        /// <summary>
        /// Gets the context providing state information for the simulation.
        /// </summary>
        protected SimulationContext Context { get; private set; }

        /// <summary>
        /// Initialize this instance.
        /// </summary>
        protected virtual void Initialize()
        {
        }
    }
}

