using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Iface.Oik.Tm.Interfaces;
using Newtonsoft.Json.Linq;

namespace Iface.Oik.ArmStatus.Workers
{
  public class TmServerWorker : Worker
  {
    private Options _options;

    private TmAddr _tmStatusToSet;


    public override void Configure(JObject options)
    {
      if (options == null)
      {
        throw new Exception("Не заданы настройки");
      }
      _options = options.ToObject<Options>();
      new OptionsValidator().ValidateAndThrow(_options);

      if (_options?.WorkInterval != null)
      {
        SetWorkInterval(_options.WorkInterval.Value);
      }

      if (!TmAddr.TryParse(_options.SetStatus, out _tmStatusToSet, TmType.Status))
      {
        throw new Exception("Требуется указать корректный адрес сигнала для установки значения");
      }
    }


    private class Options
    {
      public string ServerName   { get; set; }
      public string SetStatus    { get; set; }
      public int?   WorkInterval { get; set; }
    }


    private class OptionsValidator : AbstractValidator<Options>
    {
      public OptionsValidator()
      {
        RuleFor(o => o.ServerName).NotNull().NotEmpty();
        RuleFor(o => o.SetStatus).NotNull().NotEmpty();
      }
    }


    protected override async Task DoWork()
    {
      if (IsTmServerOnline(GetTmServers(), _options.ServerName))
      {
        await SetSuccess($"Сервер \"{_options.ServerName}\" онлайн");
      }
      else
      {
        await SetFailure($"Сервер \"{_options.ServerName}\" ОФФЛАЙН");
      }
    }


    private static bool IsTmServerOnline(IEnumerable<TmServer> servers, string name)
    {
      return servers.SelectMany(server => server.Children)
                    .Any(childServer => DoesServerNameMatch(childServer.Name, name) &&
                                        childServer.State > 0);
    }


    private static bool DoesServerNameMatch(string serverName, string name)
    {
      // имя сервера выглядит, например, так: RBS (сервер)      поэтому проверяем только до символов " ("
      return serverName.StartsWith(name + " (", StringComparison.OrdinalIgnoreCase);
    }


    private async Task SetSuccess(string message)
    {
      await SetStatus(_tmStatusToSet, 1);
      LogDebug(message);
    }


    private async Task SetFailure(string message)
    {
      await SetStatus(_tmStatusToSet, 0);
      LogDebug(message);
    }
  }
}