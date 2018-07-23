using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace FSimulate.UnitTest
{
    [TestFixture()]
    public class CompositeInstructionFixture
    {
        #region Private Types

        public class TestInstruction : InstructionBase
        {
            public bool CanCompleteResult { get; set; }

            public int? CanCompleteNextTimePeriodResult { get; set; }

            public bool HasCanCompleteBeenCalled { get; set; }

            public bool HasCompleteBeenCalled { get; set; }

            public override bool CanComplete(ISimulationContext context, out long? skipFurtherChecksUntilTimePeriod)
            {
                HasCanCompleteBeenCalled = true;

                skipFurtherChecksUntilTimePeriod = CanCompleteNextTimePeriodResult;
                return CanCompleteResult;
            }

            public override void Complete(ISimulationContext context) => HasCompleteBeenCalled = true;
        }

        #endregion

        [Test()]
        public void CanComplete_ContainedInstructionsCalled_CanCompleteOnlyWhenAllContainedInstructionsCan()
        {
            using (var context = new SimulationContext())
            {
                // create a composite instruction with test instructions
                var testInstructions = new List<TestInstruction>();
                for (var i = 1; i <= 10; i++)
                {
                    testInstructions.Add(new TestInstruction() { CanCompleteResult = true, CanCompleteNextTimePeriodResult = i });
                }
                var compositeInstruction = new CompositeInstruction(testInstructions.Cast<InstructionBase>().ToList());

                // when all contained instructions can complete, the composite instruction cancomplete call returns true
                var canComplete = compositeInstruction.CanComplete(context, out var nextTimePeriodCheck);
                Assert.IsTrue(canComplete);
                Assert.IsNull(nextTimePeriodCheck);

                foreach (TestInstruction testInstruction in testInstructions)
                {
                    Assert.IsTrue(testInstruction.HasCanCompleteBeenCalled);
                }

                // when some of the contained instructions can not complete, the composite instruction can complete call returns false
                for (var i = 0; i <= 3; i++)
                {
                    testInstructions[i].CanCompleteResult = false;
                }
                canComplete = compositeInstruction.CanComplete(context, out nextTimePeriodCheck);
                Assert.IsFalse(canComplete);
                // the next time period check is the lowest of any contained instruction next period values
                Assert.AreEqual(1, nextTimePeriodCheck);

                // the next time period check value is returned as null if any contained instruction returns null
                testInstructions[0].CanCompleteNextTimePeriodResult = null;
                canComplete = compositeInstruction.CanComplete(context, out nextTimePeriodCheck);
                Assert.IsFalse(canComplete);
                Assert.IsNull(nextTimePeriodCheck);
            }
        }

        [Test()]
        public void Complete_ContainedInstructionsCalled_AllContainedInstructionsCompleted()
        {
            using (var context = new SimulationContext())
            {
                // create a composite instruction with test instructions
                var testInstructions = new List<TestInstruction>();
                for (var i = 1; i <= 10; i++)
                {
                    testInstructions.Add(new TestInstruction() { CanCompleteResult = true, CanCompleteNextTimePeriodResult = i });
                }
                var compositeInstruction = new CompositeInstruction(testInstructions.Cast<InstructionBase>().ToList());

                compositeInstruction.Complete(context);
                foreach (TestInstruction testInstruction in testInstructions)
                {
                    Assert.IsTrue(testInstruction.HasCompleteBeenCalled);
                }
            }
        }
    }
}

