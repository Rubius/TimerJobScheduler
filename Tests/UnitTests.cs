using System;
using System.Threading;
using Xunit;

using Common.Utils.TimerScheduler;

namespace Tests
{
    public class UnitTests
    {
        TimerJobScheduler _scheduler;

        public UnitTests()
        {
            _scheduler = new TimerJobScheduler();
        }

        [Fact]
        public void TestRunning()
        {
            const int TIMER_PERIOD_MSEC = 100;
            const int COUNT = 20;

            int cnt1 = 0;
            int cnt2 = 0;
            var job1 = new TimerJob(() => ++cnt1, TIMER_PERIOD_MSEC * 0.001);
            var job2 = new TimerJob(() => ++cnt2, TIMER_PERIOD_MSEC * 2 * 0.001);

            _scheduler.AddJob(job1);
            _scheduler.AddJob(job2);
            _scheduler.Start();

            Assert.Equal(0, cnt1);
            Assert.Equal(0, cnt2);

            Thread.Sleep(TIMER_PERIOD_MSEC * COUNT);

            Assert.True((cnt1 == COUNT) || (cnt1 == COUNT - 1));
            Assert.True((cnt2 == COUNT / 2) || (cnt2 == COUNT / 2 - 1));
        }

        [Fact]
        public void TestResetAllJobs()
        {
            const int TIMER_PERIOD_MSEC = 1000;

            int cnt = 0;
            var job = new TimerJob(() => ++cnt, TIMER_PERIOD_MSEC * 0.001);

            _scheduler.AddJob(job);
            _scheduler.Start();

            Thread.Sleep(TIMER_PERIOD_MSEC - 100);
            Assert.Equal(0, cnt);

            _scheduler.ResetAllJobs();

            Thread.Sleep(TIMER_PERIOD_MSEC - 100);
            Assert.Equal(0, cnt);

            Thread.Sleep(200);
            Assert.Equal(1, cnt);
        }

        [Fact]
        public void TestJobActivate()
        {
            const int TIMER_PERIOD_MSEC = 200;

            int cnt = 0;
            var job = new TimerJob(() => ++cnt, TIMER_PERIOD_MSEC * 0.001);

            _scheduler.AddJob(job);
            _scheduler.Start();

            Thread.Sleep(100);
            Assert.Equal(0, cnt);

            job.Activate();

            Thread.Sleep(100);
            Assert.Equal(1, cnt);
        }

        [Fact]
        public void TestStop()
        {
            const int TIMER_PERIOD_MSEC = 100;

            int cnt = 0;
            var job = new TimerJob(() => ++cnt, TIMER_PERIOD_MSEC * 0.001);

            _scheduler.AddJob(job);
            _scheduler.Start();

            Assert.True(_scheduler.IsStarted);

            Thread.Sleep(TIMER_PERIOD_MSEC * 10);
            Assert.True(cnt >= 9);

            _scheduler.Stop();

            Assert.False(_scheduler.IsStarted);
            Assert.True(cnt <= 10);
        }

        [Fact]
        public void TestIsInOneThread()
        {
            const int TIMER_PERIOD_MSEC = 100;

            int job1ThreadId = 0;
            int job2ThreadId = 0;

            var job1 = new TimerJob(() => job1ThreadId = Thread.CurrentThread.ManagedThreadId, TIMER_PERIOD_MSEC * 0.001);
            var job2 = new TimerJob(() => job2ThreadId = Thread.CurrentThread.ManagedThreadId, TIMER_PERIOD_MSEC * 0.001);

            _scheduler.AddJob(job1);
            _scheduler.AddJob(job2);
            _scheduler.Start();

            Thread.Sleep(TIMER_PERIOD_MSEC + 100);

            Assert.True(job1ThreadId != 0);
            Assert.True(job1ThreadId == job2ThreadId);
        }

        [Fact]
        public void TestThrowException()
        {
            const int TIMER_PERIOD_MSEC = 100;

            var job = new TimerJob(() => throw new Exception(), TIMER_PERIOD_MSEC * 0.001);

            _scheduler.AddJob(job);
            _scheduler.Start();

            Assert.Throws<AggregateException>(() =>
            {
                Thread.Sleep(TIMER_PERIOD_MSEC + 100);
                _scheduler.Task.Wait(TimeSpan.FromMilliseconds(TIMER_PERIOD_MSEC));
            });
        }
    }
}
