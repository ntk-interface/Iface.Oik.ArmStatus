using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Iface.Oik.Tm.Helpers;
using Iface.Oik.Tm.Interfaces;
using Microsoft.Extensions.Hosting;

namespace Iface.Oik.ArmStatus;

public abstract class Worker : BackgroundService
{
    private readonly IOikDataApi _api;
    private readonly WorkerCache _cache;

    private string _name = null!;

    private int _workInterval = 5000;

    protected Worker(IOikDataApi api, WorkerCache cache)
    {
        _api = api;
        _cache = cache;
    }

    public Worker SetName(string name)
    {
        _name = name;

        return this;
    }

    protected void SetWorkInterval(int workInterval)
    {
        _workInterval = workInterval;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(500, stoppingToken); // такое асинхронное ожидание даёт хосту возможность завершить инициализацию

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DoWork(stoppingToken);
            }
            catch (Exception ex)
            {
                LogError($"{ex.GetType().Name} {ex.Message}");
            }
            await Task.Delay(_workInterval, stoppingToken);
        }
    }

    protected void LogDebug(string message)
    {
        Tms.PrintDebug($"{_name}: {message}");
    }

    protected void LogError(string message)
    {
        Tms.PrintError($"{_name}: {message}");
    }

    protected async Task SetStatus(TmAddr? tmAddr, int status)
    {
        if (tmAddr == null)
        {
            return;
        }

        var (ch, rtu, point) = tmAddr.GetTuple();
        await _api.SetStatus(ch, rtu, point, status);
    }

    protected async Task SetAnalog(TmAddr? tmAddr, float value)
    {
        if (tmAddr == null)
        {
            return;
        }

        var (ch, rtu, point) = tmAddr.GetTuple();
        await _api.SetAnalog(ch, rtu, point, value);
    }

    protected IReadOnlyCollection<TmServer> GetTmServers()
    {
        return _cache.GetTmServers();
    }

    public virtual void Configure(WorkerOptions options) { }

    protected abstract Task DoWork(CancellationToken stoppingToken);
}
