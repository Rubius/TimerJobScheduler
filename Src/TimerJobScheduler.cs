using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Common.Utils.TimerScheduler
{
    /// <summary>
    /// Планировщик выполнения набора задач с заданным интервалом.
    /// Задачи, добавленные в один планировщик будут выполняются в его потоке последовательно
    /// с учетом интервала каждой задачи.
    /// </summary>
    public class TimerJobScheduler
    {
        private const int JOB_LIST_CHECK_PERIOD = 100;

        private readonly ILogger _logger;
        private readonly ConcurrentBag<TimerJob> _timerJobs;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _throwJobException;

        /// <summary>
        /// Планировщик с логированием исключений, возникающих в его задачах.
        /// </summary>
        /// <param name="logger"></param>
        public TimerJobScheduler(ILogger logger)
        {
            _logger = logger;
            _timerJobs = new ConcurrentBag<TimerJob>();
            _throwJobException = true;
        }

        /// <summary>
        /// Дефолтный планировщик.
        /// </summary>
        public TimerJobScheduler() : this(null)
        {
        }

        /// <summary>
        /// Устанавливает разрешение на выброс исключения при возникновении исключения в задаче.
        /// По-умолчанию разрешение установлено.
        /// </summary>
        /// <param name="enable"></param>
        public void ThrowJobException(bool enable)
        {
            _throwJobException = enable;
        }

        /// <summary>
        /// Флаг, того что планировщик запущен.
        /// </summary>
        public bool IsStarted { get; private set; }

        /// <summary>
        /// Возвращает System.Task планировщика, в которой выполняются все его задачи.
        /// </summary>
        public Task Task { get; private set; }

        /// <summary>
        /// Запуск планировщика.
        /// </summary>
        public void Start()
        {
            if (IsStarted)
                return;

            ResetAllJobs();

            _cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancellationTokenSource.Token;

            Task = Task.Run(() => ExecuteJobsInfinitely(cancellationToken), cancellationToken);

            IsStarted = true;
        }

        /// <summary>
        /// Сброс всех задач планировщика, без его останова.
        /// </summary>
        public void ResetAllJobs()
        {
            _cancellationTokenSource?.Cancel();

            foreach (var timerJob in _timerJobs)
            {
                timerJob.Reset();
            }
        }

        /// <summary>
        /// Останов планировщика.
        /// </summary>
        public void Stop()
        {
            if (!IsStarted)
                return;

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = default;

            IsStarted = false;
        }

        /// <summary>
        /// Добавление задачи.
        /// </summary>
        public void AddJob(TimerJob job)
        {
            _timerJobs.Add(job);
        }

        private void ExecuteJobsInfinitely(CancellationToken cancellationToken)
        {
            while (true)
            {
                // Получение времени ожидания до ближайшей задачи.
                TimerJob job = GetNearestJob();
                var delay = (job == null) ? JOB_LIST_CHECK_PERIOD : job.GetMSecToNextExecution();

                if (delay > 0)
                {
                    Thread.Sleep((int)delay);
                    continue;
                }

                ExecuteOrThrow(job);
            }
        }

        private void ExecuteOrThrow(TimerJob job)
        {
            try
            {
                job.Execute();
            }
            catch (Exception ex)
            {
                var text = $"TimerJobScheduler. Exception in job: " +
                    $"{job.CallingAssemblyName}\n{ex.Message}\n{ex.StackTrace}";
                _logger?.LogCritical(ex, text);

                if (_throwJobException)
                    throw;
            }
        }

        private TimerJob GetNearestJob()
        {
            if (_timerJobs.IsEmpty)
                return null;

            TimerJob nearestJob = _timerJobs.First();
            long minTime = nearestJob.GetMSecToNextExecution();

            foreach (var timerJob in _timerJobs.Skip(1))
            {
                long time = timerJob.GetMSecToNextExecution();
                if (time < minTime)
                {
                    nearestJob = timerJob;
                    minTime = time;
                }
            }

            return nearestJob;
        }
    }
}