using System;
using Wpf.Lib.Scheduler;

namespace MyApp
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            await using var scheduler = new SchedulerService();

            scheduler.Register(new Every1SecondTask());
            scheduler.Register(new Every100MsTask());

            scheduler.Start();

            Console.WriteLine("스케줄러 시작 (10초 실행)");

            // 역할: 1초마다 현재 태스크 상태를 조회해 콘솔에 출력합니다.
            for (int i = 0; i < 10; i++)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                PrintCurrentTaskStatuses(scheduler);
            }

            await scheduler.StopAsync();
            Console.WriteLine("스케줄러 종료");
        }

        private static void PrintCurrentTaskStatuses(SchedulerService scheduler)
        {
            var statuses = scheduler.GetTaskStatuses();

            Console.WriteLine("--- 현재 Task 상태 ---");
            foreach (var status in statuses)
            {
                var resultText = status.LastRunSucceeded switch
                {
                    true => "성공",
                    false => "실패",
                    null => "미실행"
                };

                var runningText = status.IsRunning ? "실행중" : "대기";
                var lastStartedText = status.LastStartedAt?.ToString("HH:mm:ss.fff") ?? "-";
                var lastCompletedText = status.LastCompletedAt?.ToString("HH:mm:ss.fff") ?? "-";

                Console.WriteLine(
                    $"[{status.Name}] 상태={runningText}, 실행수={status.RunCount}, 오류수={status.ErrorCount}, 마지막시작={lastStartedText}, 마지막완료={lastCompletedText}, 마지막결과={resultText}, 마지막오류={status.LastError ?? "-"}");
            }
            Console.WriteLine();
        }
    }

    public sealed class Every1SecondTask : ISchedulerTask
    {
        public string Name => "Every1Second";
        public bool IsEnabled { get; set; } = true;
        public TimeSpan Period => TimeSpan.FromSeconds(1);

        public Task ExecuteAsync(SchedulerContext context, CancellationToken cancellationToken)
        {
            Console.WriteLine($"[{context.Now:HH:mm:ss.fff}] 1초 Task 실행");
            return Task.CompletedTask;
        }
    }

    public sealed class Every100MsTask : ISchedulerTask
    {
        public string Name => "Every100Ms";
        public bool IsEnabled { get; set; } = true;
        public TimeSpan Period => TimeSpan.FromMilliseconds(100);

        public Task ExecuteAsync(SchedulerContext context, CancellationToken cancellationToken)
        {
            Console.WriteLine($"[{context.Now:HH:mm:ss.fff}] 100ms Task 실행");
            return Task.CompletedTask;
        }
    }
}