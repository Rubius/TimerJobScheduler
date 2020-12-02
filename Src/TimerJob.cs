using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Common.Utils.TimerScheduler
{
    /// <summary>
    /// Задача, которая должна выполняться с определенным периодом, не меньше чем MIN_PERIOD_MSEC.
    /// </summary>
    public class TimerJob
    {
        private const int MIN_PERIOD_MSEC = 100;

        private readonly Action _action;
        private readonly int _periodMSec;

        private long _executionsCount;
        private long _startedTimeMSec;

        private readonly object _locker;
        private bool _isLocked;

        /// <summary>
        /// Создание задачи для синхронного выполнения action.
        /// </summary>
        /// <param name="periodSec">Период в секундах</param>
        public TimerJob(Action action, double periodSec)
        {
            _locker = new object();

            CallingAssemblyName = Assembly.GetCallingAssembly().FullName;

            if (IsAsyncAppliedToDelegate(action))
                throw new Exception($"TimerJob. Action can`t be async in {CallingAssemblyName}");

            _action = action;
            _periodMSec = Math.Max((int)(periodSec * 1000), MIN_PERIOD_MSEC);

            Reset();
        }

        /// <summary>
        /// Создание задачи для синхронного выполнения asyncAction.
        /// Асинхронное действие asyncAction преобразуются в синхронное для исполнения на заданном потоке исполнения
        /// </summary>
        /// <param name="periodSec">Период в секундах</param>
        public TimerJob(Func<Task> asyncAction, double periodSec)
            : this(() => asyncAction().GetAwaiter().GetResult(), periodSec)
        {
        }

        /// <summary>
        /// Имя вызывающей сборки.
        /// </summary>
        public string CallingAssemblyName { get; }

        /// <summary>
        /// Текущее время в миллисекундах.
        /// </summary>
        /// <returns></returns>
        public static long GetNowMSec() => DateTimeOffset.Now.ToUnixTimeMilliseconds();

        /// <summary>
        /// Активация задачи. После чего она сразу готова к выполнению.
        /// </summary>
        public void Activate()
        {
            ConditionalLock(() =>
            {
                _executionsCount = 0;
                _startedTimeMSec = TimerJob.GetNowMSec() - _periodMSec;
            });
        }

        /// <summary>
        /// Сброс задачи. После чего она будет готова к выполнению через заданный период.
        /// </summary>
        public void Reset()
        {
            ConditionalLock(() =>
            {
                _executionsCount = 0;
                _startedTimeMSec = TimerJob.GetNowMSec();
            });
        }

        /// <summary>
        /// Возвращает время от текущего момента до следующего исполнения.
        /// </summary>
        /// <returns></returns>
        public long GetMSecToNextExecution()
        {
            long nextTime = 0;

            ConditionalLock(() =>
            {
                 nextTime = _startedTimeMSec + (_executionsCount + 1) * _periodMSec;
            });

            return nextTime - TimerJob.GetNowMSec();
        }

        /// <summary>
        /// Выполнение задачи.
        /// </summary>
        public void Execute()
        {
            lock (_locker)
            {
                // Доступ к публичным методам класса при выполнении _action() должен быть незалоченным, чтобы не было блокировки.
                _isLocked = true;

                _executionsCount++;
                _action();

                _isLocked = false;
            }
        }

        private static bool IsAsyncAppliedToDelegate(Delegate d)
        {
            return d.Method.GetCustomAttribute(typeof(AsyncStateMachineAttribute)) != null;
        }

        private void ConditionalLock(Action action)
        {
            if (_isLocked)
            {
                action();
            }
            else
            {
                lock (_locker)
                {
                    action();
                }
            }
        }
    }
}
