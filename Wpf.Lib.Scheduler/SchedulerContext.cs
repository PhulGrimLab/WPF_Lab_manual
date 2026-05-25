using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wpf.Lib.Scheduler
{
    public sealed class SchedulerContext
    {
        /// <summary>
        /// 역할: 스케줄러 기준 현재 시각입니다.
        /// </summary>
        public DateTimeOffset Now { get; }

        public SchedulerContext(DateTimeOffset now)
        {
            Now = now;
        }
    }
}
