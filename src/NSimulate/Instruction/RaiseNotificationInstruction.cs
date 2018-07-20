using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NSimulate.Instruction
{
    public static class RaiseNotificationInstruction
    {
        public static RaiseNotificationInstruction<TNotification> New<TNotification>(TNotification notificationToRaise)
        {
            return new RaiseNotificationInstruction<TNotification>(notificationToRaise);
        }
    }
}
