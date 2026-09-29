using System;
using System.IO;
using System.Text.Json;
using Iface.Oik.ArmStatus.Util;
using Iface.Oik.Tm.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Iface.Oik.ArmStatus;

public static class Loader
{
    private static readonly string ConfigsPath = Path.Combine(AppContext.BaseDirectory, "configs");

    public static void AddWorkers(this IServiceCollection services)
    {
        if (!Directory.Exists(ConfigsPath))
        {
            throw new Exception("Не найден каталог с файлами конфигурации");
        }

        var workersCount = 0;
        foreach (var file in Directory.GetFiles(ConfigsPath, "*.json"))
        {
            var name = Path.GetFileName(file);
            var config = ReadConfig(file, name);

            var workerName = config.Worker;
            if (string.IsNullOrWhiteSpace(workerName))
            {
                throw new Exception($"Не задан обработчик в файле {name}");
            }

            services.AddSingleton<IHostedService>(provider =>
                CreateWorker(provider, name, workerName, config)
            );

            workersCount++;
        }

        if (workersCount == 0)
        {
            throw new Exception("Не найдено ни одного файла конфигурации");
        }

        Tms.PrintMessage($"Всего файлов конфигурации: {workersCount}");
    }

    private static WorkerConfig ReadConfig(string file, string name)
    {
        try
        {
            var configText = File.ReadAllText(file);
            return JsonSerializer.Deserialize<WorkerConfig>(configText, JsonSettings.Options)
                ?? throw new Exception("Пустой файл конфигурации");
        }
        catch (Exception ex)
        {
            throw new Exception($"Ошибка при разборе файла {name}: {ex.Message}", ex);
        }
    }

    private static Worker CreateWorker(
        IServiceProvider provider,
        string name,
        string workerName,
        WorkerConfig config
    )
    {
        try
        {
            var worker = provider.GetRequiredKeyedService<Worker>(workerName);
            worker.SetName(name).Configure(new WorkerOptions(config.Options));

            return worker;
        }
        catch (Exception ex)
        {
            throw new Exception(
                $"Ошибка обработчика {workerName} в файле {name}: {ex.Message}",
                ex
            );
        }
    }
}
