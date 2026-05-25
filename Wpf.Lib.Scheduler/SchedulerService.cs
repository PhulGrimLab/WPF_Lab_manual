using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wpf.Lib.Scheduler
{
    public sealed record SchedulerTaskStatus(
        string Name,
        bool IsEnabled,
        bool IsRunning,
        DateTimeOffset? LastStartedAt,
        DateTimeOffset? LastCompletedAt,
        bool? LastRunSucceeded,
        string? LastError,
        int RunCount,
        int ErrorCount);

    public class SchedulerService : IAsyncDisposable
    {
        // 역할: ISchedulerTask 구현체들을 등록받아 주기적으로 실행하는 서비스입니다.
        private readonly List<ISchedulerTask> _tasks = new();

        // 역할: 실행 중인 태스크들을 관리하는 리스트입니다.
        private readonly List<Task> _workers = new();
        private readonly Dictionary<ISchedulerTask, TaskRuntimeState> _taskStates = new();

        private CancellationTokenSource? _cts;

        private bool _started;
        private readonly object _sync = new();
        private bool _isStarted;
        private bool _isGloballyPaused;


        public SchedulerService() 
        { 
            Debug.WriteLine("SchedulerService created.");
        }

        private sealed class TaskRuntimeState
        {
            public DateTimeOffset? LastStartedAt { get; set; }
            public DateTimeOffset? LastCompletedAt { get; set; }
            public bool? LastRunSucceeded { get; set; }
            public string? LastError { get; set; }
            public int RunCount { get; set; }
            public int ErrorCount { get; set; }
            public bool IsRunning { get; set; }
        }

        public void Register(ISchedulerTask task)
        {
            if (_started)
            {
                throw new InvalidOperationException("이미 시작된 후에는 등록할 수 없습니다.");
            }

            // 역할: 태스크를 등록하는 메서드입니다. 시작 전에만 등록이 가능합니다.
            _tasks.Add(task);

            lock (_sync)
            {
                if (!_taskStates.ContainsKey(task))
                {
                    _taskStates[task] = new TaskRuntimeState();
                }
            }
        }

        public void Start()
        {
            if (_started)
            {
                Debug.WriteLine("SchedulerService is already started.");
                return;
            }

            _started = true;
            _cts = new CancellationTokenSource();

            // 역할: 등록된 모든 태스크에 대해 실행 루프를 시작합니다.
            // 각 태스크는 자신의 주기에 따라 실행됩니다.
            // IsEnabled 속성이 true인 태스크만 실행됩니다.
            foreach (var task in _tasks.Where(t => t.IsEnabled))
            {
                _workers.Add(RunLoopAsync(task, _cts.Token));
            }
        }

        public IReadOnlyList<SchedulerTaskStatus> GetTaskStatuses()
        {
            lock (_sync)
            {
                return _tasks
                    .Select(task =>
                    {
                        _taskStates.TryGetValue(task, out var state);

                        return new SchedulerTaskStatus(
                            Name: task.Name,
                            IsEnabled: task.IsEnabled,
                            IsRunning: state?.IsRunning ?? false,
                            LastStartedAt: state?.LastStartedAt,
                            LastCompletedAt: state?.LastCompletedAt,
                            LastRunSucceeded: state?.LastRunSucceeded,
                            LastError: state?.LastError,
                            RunCount: state?.RunCount ?? 0,
                            ErrorCount: state?.ErrorCount ?? 0);
                    })
                    .OrderBy(x => x.Name, StringComparer.Ordinal)
                    .ToList();
            }
        }


        private async Task RunLoopAsync(ISchedulerTask task, CancellationToken ct)
        {
            // 역할: 주어진 태스크를 주기적으로 실행하는 루프입니다.
            using var timer = new PeriodicTimer(task.Period);

            while (await timer.WaitForNextTickAsync(ct))
            {
                // 역할: 태스크 실행 시점의 컨텍스트를 생성합니다.
                var context = new SchedulerContext(DateTimeOffset.Now);

                try
                {
                    lock (_sync)
                    {
                        if (_taskStates.TryGetValue(task, out var startedState))
                        {
                            startedState.IsRunning = true;
                            startedState.LastStartedAt = context.Now;
                            startedState.RunCount++;
                        }
                    }

                    // 역할: 태스크의 ExecuteAsync 메서드를 호출하여 작업을 수행합니다.
                    await task.ExecuteAsync(context, ct);

                    lock (_sync)
                    {
                        if (_taskStates.TryGetValue(task, out var succeededState))
                        {
                            succeededState.IsRunning = false;
                            succeededState.LastCompletedAt = DateTimeOffset.Now;
                            succeededState.LastRunSucceeded = true;
                            succeededState.LastError = null;
                        }
                    }
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    lock (_sync)
                    {
                        if (_taskStates.TryGetValue(task, out var cancelledState))
                        {
                            cancelledState.IsRunning = false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    lock (_sync)
                    {
                        if (_taskStates.TryGetValue(task, out var failedState))
                        {
                            failedState.IsRunning = false;
                            failedState.LastCompletedAt = DateTimeOffset.Now;
                            failedState.LastRunSucceeded = false;
                            failedState.LastError = ex.Message;
                            failedState.ErrorCount++;
                        }
                    }

                    Console.WriteLine($"[{task.Name}] 오류: {ex.Message}");
                }
            }
        }

        public async Task StopAsync()
        {
            if (!_started || _cts is null) return;

            _cts.Cancel();

            try
            {
                // 역할: 모든 실행 중인 태스크 루프가 종료될 때까지 기다립니다.
                await Task.WhenAll(_workers);
            }
            catch (OperationCanceledException) { }

            _workers.Clear();
            _cts.Dispose();
            _cts = null;
            _started = false;
        }

        public async ValueTask DisposeAsync() => await StopAsync();
    }
}
