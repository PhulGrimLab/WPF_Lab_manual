using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wpf.Lib.Scheduler
{
    public class SchedulerService : IAsyncDisposable
    {
        // 역할: ISchedulerTask 구현체들을 등록받아 주기적으로 실행하는 서비스입니다.
        private readonly List<ISchedulerTask> _tasks = new();

        // 역할: 실행 중인 태스크들을 관리하는 리스트입니다.
        private readonly List<Task> _workers = new();

        private CancellationTokenSource? _cts;

        private bool _started;
        public SchedulerService() 
        { 
            Debug.WriteLine("SchedulerService created.");
        }

        public void Register(ISchedulerTask task)
        {
            if (_started)
            {
                throw new InvalidOperationException("이미 시작된 후에는 등록할 수 없습니다.");
            }

            // 역할: 태스크를 등록하는 메서드입니다. 시작 전에만 등록이 가능합니다.
            _tasks.Add(task);
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

        private static async Task RunLoopAsync(ISchedulerTask task, CancellationToken ct)
        {
            // 역할: 주어진 태스크를 주기적으로 실행하는 루프입니다.
            using var timer = new PeriodicTimer(task.Period);

            while (await timer.WaitForNextTickAsync(ct))
            {
                // 역할: 태스크 실행 시점의 컨텍스트를 생성합니다.
                var context = new SchedulerContext(DateTimeOffset.Now);

                try
                {
                    // 역할: 태스크의 ExecuteAsync 메서드를 호출하여 작업을 수행합니다.
                    await task.ExecuteAsync(context, ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
                catch (Exception ex)
                {
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
