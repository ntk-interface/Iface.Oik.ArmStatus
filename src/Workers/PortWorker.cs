using System;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using FluentValidation;
using Iface.Oik.Tm.Interfaces;
using Newtonsoft.Json.Linq;

namespace Iface.Oik.ArmStatus.Workers
{
  public class PortWorker : Worker
  {
    private const int DefaultTimeout = 500;

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
      public string Host         { get; set; }
      public int?   Port         { get; set; }
      public string SetStatus    { get; set; }
      public int?   Timeout      { get; set; }
      public int?   WorkInterval { get; set; }
    }


    private class OptionsValidator : AbstractValidator<Options>
    {
      public OptionsValidator()
      {
        RuleFor(o => o.Host).NotNull().NotEmpty();
        RuleFor(o => o.Port).NotNull().NotEmpty();
        RuleFor(o => o.SetStatus).NotNull().NotEmpty();
      }
    }


    protected override async Task DoWork()
    {
      try
      {
        using var tcpClient = new TcpClient();

        var conn = tcpClient.BeginConnect(_options.Host, _options.Port.Value, null, null);
        var isPortOpen = conn.AsyncWaitHandle.WaitOne(_options.Timeout ?? DefaultTimeout);
        tcpClient.EndConnect(conn);

        if (isPortOpen)
        {
          await SetSuccess($"Устройство \"{_options.Host}\" отвечает по порту {_options.Port}");
        }
        else
        {
          await SetFailure($"Устройство \"{_options.Host}\" НЕ отвечает по порту {_options.Port}");
        }
      }
      catch (Exception ex)
      {
        await SetFailure($"Ошибка проверки устройства \"{_options.Host}\" по порту {_options.Port}: {ex.Message}");
      }
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