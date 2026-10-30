using TaskCoordinator;

var service = new DiagnosticService();

while (true)
{
    Console.WriteLine("\n1 — три диагностики; 2 — отмена; 3 — тайм-аут; 4 — WhenAny; 0 — выход");
    var cmd = Console.ReadLine()?.Trim();
    if (cmd == "0") break;

    try
    {
        if (cmd == "1")
        {
            foreach (var x in await service.RunAllAsync())
                Console.WriteLine($"{x.Name}: {x.Status} ({x.Duration.TotalMilliseconds:F0} мс) - {x.Details}");
        }
        else if (cmd == "2")
        {
            using var cts = new CancellationTokenSource();
            var task = service.RunAsync("Долгая", TimeSpan.FromSeconds(5), cts.Token);

            await Task.Delay(400); 
            cts.Cancel();

            var result = await task;
            Console.WriteLine($"Результат: {result.Status} - {result.Details}");
        }
        else if (cmd == "3")
        {
            try
            {
                var result = await service.RunWithTimeoutAsync(
                    "Тайм-аут",
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(1)
                );
                Console.WriteLine($"Результат: {result.Status} - {result.Details}");
            }
            catch (TimeoutException ex)
            {
                Console.WriteLine($"[TIMEOUT] {ex.Message}");
            }
        }
        else if (cmd == "4")
        {
            var first = await service.FirstFinishedAsync();
            Console.WriteLine($"Первой завершилась: {first}");
        }
        else
        {
            Console.WriteLine("Неизвестная команда.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ОШИБКА] {ex.GetType().Name}: {ex.Message}");
    }
}