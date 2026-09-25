using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Iface.Oik.ArmStatus.Util;
using Iface.Oik.Tm.Interfaces;

namespace Iface.Oik.ArmStatus.Workers;

public class TmClientWorker : Worker
{
    private Options _options;

    private TmAddr _tmStatusToSet;

    public override void Configure(WorkerOptions options)
    {
        _options = options.Get<Options>();
        OptionsGuard.ThrowIfNullOrEmpty(_options.ClientName, "ClientName");
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
        public string ClientName { get; init; }
        public string ServerName { get; init; }
        public string SetStatus { get; init; }
        public int? WorkInterval { get; init; }
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

    private static bool IsTmClientOnline(
        IEnumerable<TmServer> servers,
        string clientName,
        string serverName
    )
    {
        return servers
            .SelectMany(server => server.Children)
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
        if (
            userName.StartsWith("mon$")
            || // ТМС-монитор
            userName == "__TMC__"
        )
        {
            return false;
        }

        return string.Equals(name, userName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, userComment, StringComparison.OrdinalIgnoreCase)
            || string.Equals(
                name,
                userName + " " + userComment,
                StringComparison.OrdinalIgnoreCase
            );
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
