using System.Diagnostics;

namespace TaskCoordinator;

public sealed class DiagnosticService
{
    public async Task<DiagnosticResult> RunAsync(string name, TimeSpan duration, CancellationToken token = default)
    {
        // TODO 1: Проверить имя и неотрицательную длительность
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Имя диагностики не может быть пустым.", nameof(name));
        if (duration < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(duration), "Длительность не может быть отрицательной.");

        var sw = Stopwatch.StartNew();
        try
        {
            // TODO 2: Передать token в Task.Delay
            await Task.Delay(duration, token);
            sw.Stop();

            // TODO 3: Вернуть статус Completed и длительность
            return new DiagnosticResult(name, DiagnosticStatus.Completed, "Успешно", sw.Elapsed);
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            // TODO 10: Возвращаем статус Cancelled вместо проброса исключения
            return new DiagnosticResult(name, DiagnosticStatus.Cancelled, "Операция отменена", sw.Elapsed);
        }
        catch (Exception ex)
        {
            sw.Stop();
            // TODO 10: Возвращаем статус Failed и текст ошибки
            return new DiagnosticResult(name, DiagnosticStatus.Failed, ex.Message, sw.Elapsed);
        }
    }

    public async Task<DiagnosticResult> RunCpuBoundAsync(string name, int iterations, CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Имя не может быть пустым.", nameof(name));
        if (iterations < 0)
            throw new ArgumentOutOfRangeException(nameof(iterations), "Количество итераций не может быть отрицательным.");

        var sw = Stopwatch.StartNew();
        try
        {
            // TODO 4: Перенести CPU-bound цикл в Task.Run; регулярно проверять token
            long sum = await Task.Run(() =>
            {
                long localSum = 0;
                for (int i = 0; i < iterations; i++)
                {
                    token.ThrowIfCancellationRequested(); 
                    localSum += i % 7;
                }
                return localSum;
            }, token);

            sw.Stop();
            return new DiagnosticResult(name, DiagnosticStatus.Completed, $"Сумма: {sum}", sw.Elapsed);
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            return new DiagnosticResult(name, DiagnosticStatus.Cancelled, "Операция отменена", sw.Elapsed);
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new DiagnosticResult(name, DiagnosticStatus.Failed, ex.Message, sw.Elapsed);
        }
    }

    public async Task<IReadOnlyList<DiagnosticResult>> RunAllAsync(CancellationToken token = default)
    {
        var tasks = new[]
        {
            RunAsync("Сеть", TimeSpan.FromMilliseconds(600), token),
            RunAsync("Диск", TimeSpan.FromMilliseconds(900), token),
            RunAsync("База", TimeSpan.FromMilliseconds(700), token)
        };

        // TODO 5: Дождаться всех задач через WhenAll
        await Task.WhenAll(tasks);

        return tasks.Select(t => t.Result).ToList();
    }

    public async Task<DiagnosticResult> RunWithTimeoutAsync(string name, TimeSpan duration, TimeSpan timeout, CancellationToken token = default)
    {
        // TODO 6: Создать linked CancellationTokenSource на основе внешнего token
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token);

        // TODO 7: Настроить CancelAfter(timeout)
        linkedCts.CancelAfter(timeout);

        var result = await RunAsync(name, duration, linkedCts.Token);

        // TODO 8: Отличить тайм-аут от отмены вызывающей стороны
        if (result.Status == DiagnosticStatus.Cancelled && linkedCts.IsCancellationRequested && !token.IsCancellationRequested)
        {
            throw new TimeoutException($"Диагностика '{name}' превысила лимит времени ({timeout.TotalSeconds} сек.).");
        }
        return result;
    }

    public async Task<string> FirstFinishedAsync(CancellationToken token = default)
    {
        var tasks = new[]
        {
            RunAsync("Быстрая", TimeSpan.FromMilliseconds(150), token),
            RunAsync("Средняя", TimeSpan.FromMilliseconds(350), token),
            RunAsync("Медленная", TimeSpan.FromMilliseconds(600), token)
        };

        // TODO 9: Использовать WhenAny, затем await первой задачи
        var firstTask = await Task.WhenAny(tasks);
        var firstResult = await firstTask;
        return firstResult.Name;
    }
}