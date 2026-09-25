using System;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using Iface.Oik.ArmStatus.Util;
using Iface.Oik.Tm.Interfaces;

namespace Iface.Oik.ArmStatus.Workers;

public class PortWorker : Worker
{
    private const int DefaultTimeout = 500;

    private Options _options;

    private TmAddr _tmStatusToSet;

    public override void Configure(WorkerOptions options)
    {
        _options = options.Get<Options>();
        OptionsGuard.ThrowIfNullOrEmpty(_options.Host, "Host");
        OptionsGuard.ThrowIfNull(_options.Port, "Port");
        OptionsGuard.ThrowIfNullOrEmpty(_options.SetStatus, "SetStatus");

        if (!TmAddr.TryParse(_options.SetStatus, out _tmStatusToSet, TmType.Status))
        {
            throw new Exception(
                "Требуется указать корректный адрес сигнала для установки значения"
            );
        }

        if (_options.WorkInterval != null)
        {
            SetWorkInterval(_options.WorkInterval.Value);
        }
    }

    private class Options
    {
        public string Host { get; init; }
        public int? Port { get; init; }
        public string SetStatus { get; init; }
        public int? Timeout { get; init; }
        public int? WorkInterval { get; init; }
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
                await SetSuccess(
                    $"Устройство \"{_options.Host}\" отвечает по порту {_options.Port}"
                );
            }
            else
            {
                await SetFailure(
                    $"Устройство \"{_options.Host}\" НЕ отвечает по порту {_options.Port}"
                );
            }
        }
        catch (Exception ex)
        {
            await SetFailure(
                $"Ошибка проверки устройства \"{_options.Host}\" по порту {_options.Port}: {ex.Message}"
            );
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
