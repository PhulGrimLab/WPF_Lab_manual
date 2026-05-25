using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wpf.Lib.Scheduler
{
    public interface ISchedulerTask
    {
        string Name { get; }
        bool IsEnabled { get; set; }

        TimeSpan Period { get; }

        /// <summary>
        /// 태스크 본문을 실행합니다.
        /// </summary>
        /// <param name="context">현재 시각 및 양보 판단을 담은 실행 컨텍스트입니다.</param>
        /// <param name="cancellationToken">중지 요청 전달 토큰입니다.</param>
        Task ExecuteAsync(SchedulerContext context, CancellationToken cancellationToken);
    }
}
