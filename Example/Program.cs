using Common.Utils.TimerScheduler;
using Microsoft.Extensions.Logging;
using System;

namespace Example
{
    class Program
    {
        static void Main(string[] args)
        {
            int cnt1 = 0;
            int cnt2 = 0;

            var job1 = new TimerJob(() => Func("Job1", ref cnt1), 0.5);
            var job2 = new TimerJob(() => Func("Job2", ref cnt2), 1.0);
            var job3 = new TimerJob(() => throw new Exception("Job3"), 2.0);

            var scheduler = new TimerJobScheduler(GetLogger());
            scheduler.ThrowJobException(false);

            scheduler.AddJob(job1);
            scheduler.AddJob(job2);
            scheduler.AddJob(job3);

            scheduler.Start();

            Console.ReadKey();
            Console.WriteLine($"cnt1: {cnt1};  cnt2: {cnt2};");
        }

        static void Func(string job, ref int cnt)
        {
            Console.WriteLine(job);
            cnt++;
        }

        static ILogger GetLogger()
        {
            using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            return loggerFactory.CreateLogger<Program>();
        }
    }
}
