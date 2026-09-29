using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Iface.Oik.ArmStatus.Util;
using Iface.Oik.Tm.Interfaces;

namespace Iface.Oik.ArmStatus.Workers;

public class PortWorker : Worker
{
    private const int DefaultTimeout = 500;

    private Options _options = null!;

    private TmAddr _tmStatusToSet = null!;

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
        public string Host { get; init; } = null!;
        public int? Port { get; init; }
        public string SetStatus { get; init; } = null!;
        public int? Timeout { get; init; }
        public int? WorkInterval { get; init; }
    }

    protected override async Task DoWork(CancellationToken stoppingToken)
    {
        var host = _options.Host;
        var port = _options.Port!.Value;
        var timeout = _options.Timeout ?? DefaultTimeout;

        using var tcpClient = new TcpClient();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        cts.CancelAfter(timeout);

        try
        {
            await tcpClient.ConnectAsync(host, port, cts.Token);

            await SetSuccess($"Устройство \"{host}\" отвечает по порту {port}");
        }
        catch (Exception) when (stoppingToken.IsCancellationRequested)
        {
            // остановка приложения — статус не меняем
        }
        catch (Exception) when (cts.IsCancellationRequested)
        {
            await SetFailure(
                $"Устройство \"{host}\" НЕ отвечает по порту {port} (таймаут {timeout} мс)"
            );
        }
        catch (Exception ex)
        {
            await SetFailure(
                $"Ошибка проверки устройства \"{host}\" по порту {port}: {ex.Message}"
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
