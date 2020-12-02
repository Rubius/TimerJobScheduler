# Timer job scheduler
Allows you to run a set of tasks with a specified period and in one thread. They will be thread safe to each other.

Небольшая библиотека, позволяющая выполнять набор задач пользователя в едином потоке диспетчера задач. Все методы вызываемые из этих задач будут гарантированно потокобезопасными по отношению друг к другу.

Пример использования:
```csharp
static void Main(string[] args)
{
    int cnt1 = 0;
    int cnt2 = 0;

    var job1 = new TimerJob(() => Func("Job1", ref cnt1), 0.5);
    var job2 = new TimerJob(() => Func("Job2", ref cnt2), 1.0);

    var scheduler = new TimerJobScheduler();
    scheduler.ThrowJobException(false);

    scheduler.AddJob(job1);
    scheduler.AddJob(job2);
    scheduler.Start();

    Console.ReadKey();
    Console.WriteLine($"cnt1: {cnt1};  cnt2: {cnt2};");
}

static void Func(string job, ref int cnt)
{
    Console.WriteLine(job);
    cnt++;
}
```

Возможно добавление произвольного количества диспетчеров и задач в них. 
Возможно логирование исключений, возникающих в задачах, для этого нужно передать логгер в конструктор диспетчера:
```csharp
public TimerJobScheduler(ILogger logger)
```
При помощи метода диспетчера:
```csharp
public void ThrowJobException(bool enable)
```
возможно разрешение или запрет дальнейшей передачи исключений от задач в систему.
Получить объект System.Task планировщика, в котором выполняются пользовательские задачи, можно через метод:
```csharp
public Task Task { get; }
```
Подробнее о библиотеке смотри:
[Example](https://github.com/Rubius/TimerJobScheduler/tree/main/Example),
[Tests](https://github.com/Rubius/TimerJobScheduler/tree/main/Tests).
