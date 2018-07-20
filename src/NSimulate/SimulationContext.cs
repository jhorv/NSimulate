using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NSimulate
{
    /// <summary>
    /// Context containing state information for a simulation
    /// </summary>
    public class SimulationContext : IDisposable
    {
        //static SimulationContext _current;

        //[ThreadStatic]
        //static SimulationContext _currentForThread;

        Dictionary<Type, Dictionary<object, SimulationElement>> _registeredElements = new Dictionary<Type, Dictionary<object, SimulationElement>>();

        ///// <summary>
        ///// Gets the current SimulationContext.  A new SimulationContext is created if one does not already exist.
        ///// </summary>
        //public static SimulationContext Current => _currentForThread ?? _current;

        public SimulationContext()
            //: this(false)
        {
        }

        public long TimePeriod { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether the simulation is terminating.
        /// </summary>
        public bool IsSimulationTerminating { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the simulation is stopping.
        /// </summary>
        public bool IsSimulationStopping { get; set; }

        /// <summary>
        /// Gets the active processes.
        /// </summary>
        public HashSet<Process> ActiveProcesses { get; private set; }

        /// <summary>
        /// Gets the processes remaining this time period.
        /// </summary>
        public Queue<Process> ProcessesRemainingThisTimePeriod { get; private set; }

        /// <summary>
        /// Gets the processed processes.
        /// </summary>
        public HashSet<Process> ProcessedProcesses { get; private set; }

        /// <summary>
        /// Moves to time period.
        /// </summary>
        /// <param name='timePeriod'>
        /// Time period.
        /// </param>
        public void MoveToTimePeriod(long timePeriod)
        {
            TimePeriod = timePeriod;

            if (ActiveProcesses == null)
            {
                ActiveProcesses = new HashSet<Process>();

                IEnumerable<Process> processes = GetByType<Process>();

                foreach (Process process in processes)
                {
                    if (process.SimulationState == null)
                    {
                        process.SimulationState = new ProcessSimulationState();
                    }

                    if (process.SimulationState.IsActive)
                    {
                        ActiveProcesses.Add(process);
                    }
                }
            }

            // order processes by process priority, instruction priority, insruction raise time, and then process instance
            IOrderedEnumerable<Process> processesInPriorityOrder =
                ActiveProcesses
                    .OrderBy(p => p.Priority)
                    .ThenBy(p => (p.SimulationState != null && p.SimulationState.InstructionEnumerator != null && p.SimulationState.InstructionEnumerator.Current != null)
                             ? p.SimulationState.InstructionEnumerator.Current.Priority
                             : Priority.Medium)
                    .ThenBy(p => (p.SimulationState != null && p.SimulationState.InstructionEnumerator != null && p.SimulationState.InstructionEnumerator.Current != null)
                             ? p.SimulationState.InstructionEnumerator.Current.RaisedInTimePeriod
                             : timePeriod)
                    .ThenBy(p => p.GetInstanceIndex());

            ProcessesRemainingThisTimePeriod = new Queue<Process>(ActiveProcesses.Count);
            foreach (Process process in processesInPriorityOrder)
            {
                ProcessesRemainingThisTimePeriod.Enqueue(process);
            }
            ProcessedProcesses = new HashSet<Process>();
        }

        ///// <summary>
        ///// Initializes a new instance of the <see cref="NSimulate.SimulationContext"/> class.
        ///// </summary>
        ///// <param name="isDefaultContextForProcess">if true, this context will become the default for the process</param>
        ///// <param name="isDefaultContextForThread">if true, this context will become the default for the current thread</param>
        //public SimulationContext(bool isDefaultContextForProcess, bool isDefaultContextForThread = false)
        //{
        //    if (isDefaultContextForProcess)
        //    {
        //        _current = this;
        //    }

        //    if (isDefaultContextForThread)
        //    {
        //        _currentForThread = this;
        //    }
        //}

        /// <summary>
        /// Register an object with this context
        /// </summary>
        public void Register<TType>(TType objectToRegister)
            where TType : SimulationElement => Register(typeof(TType), objectToRegister);

        /// <summary>
        /// Register an object with this context
        /// </summary>
        public void Register(Type typeToRegister, object objectToRegister)
        {
            if (!IsTypeEqualOrSubclass(typeToRegister, typeof(SimulationElement)))
            {
                throw new ArgumentException("typeToRegister");
            }

            var element = objectToRegister as SimulationElement;


            if (!_registeredElements.TryGetValue(typeToRegister, out Dictionary<object, SimulationElement> elementsByKey))
            {
                elementsByKey = new Dictionary<object, SimulationElement>();
                _registeredElements[typeToRegister] = elementsByKey;
            }

            elementsByKey[element.Key] = element;
        }

        /// <summary>
        /// Gets a previously registered object by key.
        /// </summary>
        /// <returns>
        /// The matching object if any
        /// </returns>
        /// <param name='key'>
        /// Key identifying object
        /// </param>
        /// <typeparam name='TType'>
        /// The type of object to retrieve
        /// </typeparam>
        public TType GetByKey<TType>(object key)
            where TType : SimulationElement
        {
            SimulationElement objectToRetrieve = null;

            foreach (KeyValuePair<Type, Dictionary<object, SimulationElement>> entry in _registeredElements)
            {
                if (IsTypeEqualOrSubclass(entry.Key, typeof(TType)))
                {
                    if (entry.Value.TryGetValue(key, out objectToRetrieve))
                    {
                        break;
                    }
                }
            }

            return objectToRetrieve as TType;
        }

        /// <summary>
        /// Get a list of objects registered
        /// </summary>
        /// <returns>
        /// A list of matching objects
        /// </returns>
        /// <typeparam name='TType'>
        /// The type of objects to retrieve
        /// </typeparam>
        public IEnumerable<TType> GetByType<TType>()
        {
            var enumerableToReturn = new List<TType>();

            foreach (KeyValuePair<Type, Dictionary<object, SimulationElement>> entry in _registeredElements)
            {
                if (IsTypeEqualOrSubclass(entry.Key, typeof(TType)))
                {
                    enumerableToReturn.AddRange(entry.Value.Values.Cast<TType>());
                }
            }

            return enumerableToReturn;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>
        /// 2
        /// </filterpriority>
        /// <remarks>
        /// Call <see cref="Dispose"/> when you are finished using the <see cref="NSimulate.SimulationContext"/>. The
        /// <see cref="Dispose"/> method leaves the <see cref="NSimulate.SimulationContext"/> in an unusable state. After
        /// calling <see cref="Dispose"/>, you must release all references to the <see cref="NSimulate.SimulationContext"/> so
        /// the garbage collector can reclaim the memory that the <see cref="NSimulate.SimulationContext"/> was occupying.
        /// </remarks>
        public void Dispose()
        {
            //if (_current == this)
            //{
            //    _current = null;
            //}

            //if (_currentForThread == this)
            //{
            //    _currentForThread = null;
            //}
        }

        /// <summary>
        /// Tests whether a Type is the same as another, or the subclass of another
        /// </summary>
        /// <param name='typeToCheck'>
        /// The type to be checked
        /// </param>
        /// <param name='typeToCompare'>
        /// The type to match, either exactly or as an ancestor
        /// </param>
        /// <returns>True if the typeToCheck is the same or a subclass of the typeToCompare</returns>
        private bool IsTypeEqualOrSubclass(Type typeToCheck, Type typeToCompare)
        {
            var match = false;

            Type currentType = typeToCheck;

            while (currentType != null)
            {

                if (currentType == typeToCompare)
                {
                    match = true;
                    break;
                }

                currentType = currentType.GetTypeInfo().BaseType;
            }

            return match;
        }
    }
}

