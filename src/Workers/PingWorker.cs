using System;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using FluentValidation;
using Iface.Oik.Tm.Interfaces;
using Newtonsoft.Json.Linq;

namespace Iface.Oik.ArmStatus.Workers
{
  public class PingWorker : Worker
  {
    private const int   DefaultTimeout                = 500;
    private const float PingFailureRoundtripTimeValue = -1;

    private Options _options;

    private TmAddr _tmStatusToSet;
    private TmAddr _tmAnalogToSet;


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

      TmAddr.TryParse(_options.SetStatus, out _tmStatusToSet, TmType.Status);
      TmAddr.TryParse(_options.SetAnalog, out _tmAnalogToSet, TmType.Analog);

      if (_tmStatusToSet == null && _tmAnalogToSet == null)
      {
        throw new Exception("Требуется указать либо адрес сигнала, либо адрес измерения для установки значения");
      }
    }


    private class Options
    {
      public string Host         { get; set; }
      public string SetStatus    { get; set; }
      public string SetAnalog    { get; set; }
      public int?   Timeout      { get; set; }
      public int?   WorkInterval { get; set; }
    }


    private class OptionsValidator : AbstractValidator<Options>
    {
      public OptionsValidator()
      {
        RuleFor(o => o.Host).NotNull().NotEmpty();
      }
    }


    protected override async Task DoWork()
    {
      try
      {
        using var pingService = new Ping();

        var reply = await pingService.SendPingAsync(_options.Host,
                                                    _options.Timeout ?? DefaultTimeout);
        if (reply.Status == IPStatus.Success)
        {
          await SetSuccess(reply.RoundtripTime,
                           $"Устройство \"{_options.Host}\" онлайн, время ответа: {reply.RoundtripTime} мс");
        }
        else
        {
          await SetFailure($"Устройство \"{_options.Host}\" ОФФЛАЙН");
        }
      }
      catch (Exception ex)
      {
        await SetFailure($"Ошибка команды пинга устройства \"{_options.Host}\": {ex.Message}");
      }
    }


    private async Task SetSuccess(long roundtripTime, string message)
    {
      await SetStatus(_tmStatusToSet, 1);
      await SetAnalog(_tmAnalogToSet, roundtripTime);

      LogDebug(message);
    }


    private async Task SetFailure(string message)
    {
      await SetStatus(_tmStatusToSet, 0);
      await SetAnalog(_tmAnalogToSet, PingFailureRoundtripTimeValue);

      LogDebug(message);
    }
  }
}