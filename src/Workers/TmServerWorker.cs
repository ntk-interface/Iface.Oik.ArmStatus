using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Iface.Oik.ArmStatus.Util;
using Iface.Oik.Tm.Interfaces;

namespace Iface.Oik.ArmStatus.Workers;

public class TmServerWorker : Worker
{
    private Options _options;

    private TmAddr _tmStatusToSet;

    public override void Configure(WorkerOptions options)
    {
        _options = options.Get<Options>();
        OptionsGuard.ThrowIfNullOrEmpty(_options.ServerName, "ServerName");
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
        public string ServerName { get; init; }
        public string SetStatus { get; init; }
        public int? WorkInterval { get; init; }
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
        return servers
            .SelectMany(server => server.Children)
            .Any(childServer =>
                DoesServerNameMatch(childServer.Name, name) && childServer.State > 0
            );
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
