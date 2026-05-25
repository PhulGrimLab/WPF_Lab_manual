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

            Console.WriteLine("스케줄러 시작 (5초 실행)");
            await Task.Delay(TimeSpan.FromSeconds(10));
            await scheduler.StopAsync();
            Console.WriteLine("스케줄러 종료");
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