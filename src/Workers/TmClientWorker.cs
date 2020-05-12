using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Iface.Oik.Tm.Interfaces;
using Newtonsoft.Json.Linq;

namespace Iface.Oik.ArmStatus.Workers
{
  public class TmClientWorker : Worker
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
      public string ClientName   { get; set; }
      public string ServerName   { get; set; }
      public string SetStatus    { get; set; }
      public int?   WorkInterval { get; set; }
    }


    private class OptionsValidator : AbstractValidator<Options>
    {
      public OptionsValidator()
      {
        RuleFor(o => o.ClientName).NotNull().NotEmpty();
        RuleFor(o => o.SetStatus).NotNull().NotEmpty();
      }
    }


    protected override async Task DoWork()
    {
      if (IsTmClientOnline(GetTmServers(), _options.ClientName, _options.ServerName))
      {
        await SetSuccess($"Клиент \"{_options.ClientName}\" онлайн");
      }
      else
      {
        await SetFailure($"Клиент \"{_options.ClientName}\" ОФФЛАЙН");
      }
    }


    private static bool IsTmClientOnline(IEnumerable<TmServer> servers, string clientName, string serverName)
    {
      return servers.SelectMany(server => server.Children)
                    .Where(childServer => DoesServerNameMatch(childServer.Name, serverName))
                    .SelectMany(childServer => childServer.Users)
                    .Any(user => DoesClientNameMatch(user.Name, user.Comment, clientName));
    }


    private static bool DoesServerNameMatch(string serverName, string name)
    {
      if (name == null) // если имя сервера не задано, то подходит любой
      {
        return true;
      }
      // имя сервера выглядит, например, так: RBS (сервер)      поэтому проверяем только до символов " ("
      return serverName.StartsWith(name + " (", StringComparison.OrdinalIgnoreCase);
    }


    private static bool DoesClientNameMatch(string userName, string userComment, string name)
    {
      if (userName.StartsWith("mon$") || // ТМС-монитор
          userName == "__TMC__")
      {
        return false;
      }

      return string.Equals(name, userName,                     StringComparison.OrdinalIgnoreCase) ||
             string.Equals(name, userComment,                  StringComparison.OrdinalIgnoreCase) ||
             string.Equals(name, userName + " " + userComment, StringComparison.OrdinalIgnoreCase);
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