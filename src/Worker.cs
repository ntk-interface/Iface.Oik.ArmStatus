using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Iface.Oik.Tm.Helpers;
using Iface.Oik.Tm.Interfaces;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Linq;

namespace Iface.Oik.ArmStatus
{
  public abstract class Worker : BackgroundService
  {
    private string      _name;
    private IOikDataApi _api;
    private WorkerCache _cache;
    private int         _workInterval = 5000;


    public Worker SetName(string name)
    {
      _name = name;

      return this;
    }


    public Worker SetApis(IOikDataApi api, WorkerCache cache)
    {
      _api   = api;
      _cache = cache;

      return this;
    }


    public void SetWorkInterval(int workInterval)
    {
      _workInterval = workInterval;
    }


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      await Task.Delay(500, stoppingToken);

      while (!stoppingToken.IsCancellationRequested)
      {
        await DoWork();
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


    protected async Task SetStatus(TmAddr tmAddr, int status)
    {
      if (tmAddr == null)
      {
        return;
      }

      var (ch, rtu, point) = tmAddr.GetTuple();
      await _api.SetStatus(ch, rtu, point, status);
    }


    protected async Task SetAnalog(TmAddr tmAddr, float value)
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


    public virtual void Configure(JObject options)
    {
    }


    protected abstract Task DoWork();
  }
}