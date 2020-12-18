using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Iface.Oik.Tm.Helpers;
using Iface.Oik.Tm.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;

namespace Iface.Oik.ArmStatus
{
  public static class Loader
  {
    private static readonly string ConfigsPath = Path.Combine(AppContext.BaseDirectory, "configs");


    public static bool AddWorkers(this IServiceCollection services)
    {
      if (!Directory.Exists(ConfigsPath))
      {
        Tms.PrintError("Не найден каталог с файлами конфигурации");
        return false;
      }

      var allWorkers = FindAllWorkers();

      var workersCount = 0;
      foreach (var file in Directory.GetFiles(ConfigsPath, "*.json"))
      {
        var name = Path.GetFileName(file);
        try
        {
          var worker = CreateWorker(allWorkers, name, File.ReadAllText(file));
          services.AddSingleton<IHostedService>(provider => worker.Initialize(provider.GetService<IOikDataApi>(),
                                                                              provider.GetService<WorkerCache>()));

          workersCount++;
        }
        catch (JsonException ex)
        {
          Tms.PrintError($"Ошибка JSON при разборе файла {name}: {ex.Message}");
        }
        catch (Exception ex)
        {
          Tms.PrintError($"Ошибка при разборе файла {name}: {ex.Message}");
        }
      }

      if (workersCount == 0)
      {
        Tms.PrintError("Не найдено ни одного файла конфигурации");
        return false;
      }

      Tms.PrintMessage($"Всего файлов конфигурации: {workersCount}");
      return true;
    }


    private static List<Type> FindAllWorkers()
    {
      return Assembly.GetExecutingAssembly()
                     .GetTypes()
                     .Where(t => t.IsSubclassOf(typeof(Worker)))
                     .ToList();
    }


    public static Worker CreateWorker(IEnumerable<Type> allWorkers, string name, string configText)
    {
      var config = JsonConvert.DeserializeObject<WorkerConfig>(configText);

      var worker = CreateWorkerInstance(allWorkers, config.Worker);
      if (worker == null)
      {
        throw new Exception($"Не найден обработчик {config.Worker}");
      }

      worker.SetName(name)
            .Configure(config.Options);

      return worker;
    }


    private static Worker CreateWorkerInstance(IEnumerable<Type> allWorkers, string name)
    {
      var type = allWorkers.FirstOrDefault(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));
      if (type == null)
      {
        return null;
      }
      return Activator.CreateInstance(type) as Worker;
    }
  }
}