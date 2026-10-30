using System.Security.Cryptography;
using TaskCoordinator;

var pass = 0;
var fail = 0;
void Check(bool ok, string testName)
{
    if (ok)
    {
        pass++;
        Console.WriteLine("PASS");
    }
    else
    {
        fail++;
        Console.WriteLine($"FAIL: {testName}");
    }
}

var s = new DiagnosticService();

// 1. Базовая проверка
var one = await s.RunAsync("Сеть", TimeSpan.FromMilliseconds(20));
Check(one.Status == DiagnosticStatus.Completed, "одна диагностика завершается");

// 2. WhenAll ждёт три диагностики
var all = await s.RunAllAsync();
Check(all.Count == 3 && all.All(x => x.Status == DiagnosticStatus.Completed), "WhenAll ждёт три диагностики");

// 3. WhenAny возвращает первую завершившуюся
var first = await s.FirstFinishedAsync();
Check(first == "Быстрая", "WhenAny возвращает первую завершившуюся");

// 4. Тест на неверное имя
try
{
    await s.RunAsync("", TimeSpan.FromMilliseconds(10));
    Check(false, "пустое имя должно вызывать ошибку");
}
catch (ArgumentException)
{
    Check(true, "пустое имя вызывает ArgumentException");
}

// 5. Тест на отрицательное время
try
{
    await s.RunAsync("Тест", TimeSpan.FromMilliseconds(-10));
    Check(false, "отрицательная задержка должна вызывать ошибку");
}
catch (ArgumentOutOfRangeException)
{
    Check(true, "отрицательная задержка вызывает ArgumentOutOfRangeException");
}

// 6. Тест на кооперативную отмену
using var cancelCts = new CancellationTokenSource();
cancelCts.Cancel(); 
var cancelResult = await s.RunAsync("Отменяемая", TimeSpan.FromSeconds(5), cancelCts.Token);
Check(cancelResult.Status == DiagnosticStatus.Cancelled, "кооперативная отмена работает");

// 7. Тест на тайм-аут
using var timeoutCts = new CancellationTokenSource();
try
{
    await s.RunWithTimeoutAsync("Тайм-аут тест", TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(50), timeoutCts.Token);
    Check(false, "должен был выбросить TimeoutException");
}
catch (TimeoutException)
{
    Check(true, "тайм-аут корректно выбрасывает TimeoutException");
}
Console.WriteLine($"\nИтого пройдено: {pass}");
if (fail > 0)
{
    Console.WriteLine($"Ошибок: {fail}");
}
return fail == 0 ? 0 : 1;