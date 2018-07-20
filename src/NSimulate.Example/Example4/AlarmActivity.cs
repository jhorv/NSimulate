using System;
using System.Collections.Generic;
using NSimulate;
using NSimulate.Instruction;

namespace NSimulate.Example4
{
	public class AlarmActivity : Activity
	{
        public override IEnumerator<InstructionBase> Simulate()
        {
            var notification = new AlarmRingingNotification();
            yield return RaiseNotificationInstruction.New(notification);
        }
    }
}

