using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Iface.Oik.Tm.Helpers;
using Iface.Oik.Tm.Interfaces;
using Microsoft.Extensions.Hosting;

namespace Iface.Oik.ArmStatus
{
  public class ServerService : CommonServerService, IHostedService
  {
  }


  public class TmStartup : BackgroundService
  {
    private const string ApplicationName = "ArmStatus";
    private const string TraceName       = "ArmStatus";
    private const string TraceComment    = "<Iface.Oik.ArmStatus>";

    private static string _host;

    private static int              _tmCid;
    private static TmUserInfo       _userInfo;
    private static TmServerFeatures _serverFeatures;
    private static IntPtr           _stopEventHandle;
    private static IntPtr           _cfCid;

    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly ICommonInfrastructure    _infr;
    private readonly ICfsApi                  _cfsApi;


    public TmStartup(ICommonInfrastructure    infr,
                     ICfsApi                  cfsApi,
                     IHostApplicationLifetime applicationLifetime)
    {
      _infr                = infr;
      _cfsApi              = cfsApi;
      _applicationLifetime = applicationLifetime;
    }


    public static void Connect()
    {
      var commandLineArgs = Environment.GetCommandLineArgs();

      _host = commandLineArgs.ElementAtOrDefault(2) ?? ".";

      (_tmCid, _userInfo, _serverFeatures, _stopEventHandle) = Tms.InitializeAsTaskWithoutSql(
        new TmOikTaskOptions
        {
          TraceName    = TraceName,
          TraceComment = TraceComment,
        },
        new TmInitializeOptions
        {
          ApplicationName = ApplicationName,
          Host            = _host,
          TmServer        = commandLineArgs.ElementAtOrDefault(1) ?? "TMS",
          User            = commandLineArgs.ElementAtOrDefault(3) ?? "",
          Password        = commandLineArgs.ElementAtOrDefault(4) ?? "",
        });

      CfsConnect();

      Tms.PrintMessage("Соединение с сервером установлено");
    }


    private static void CfsConnect()
    {
      string errorString;
      int    errorCode;
      (_cfCid, errorString, errorCode) = Cfs.ConnectToCfs(_host);
      if (!Cfs.IsConnected(_cfCid))
      {
        throw new Exception($"Нет связи с ТМ-сервером, ошибка {errorCode} - {errorString}");
      }
    }


    public override Task StartAsync(CancellationToken cancellationToken)
    {
      _infr.InitializeTmWithoutSql(_tmCid, _userInfo, _serverFeatures);
      _cfsApi.SetCfIdAndHost(_cfCid, _host);
      return base.StartAsync(cancellationToken);
    }


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      while (!stoppingToken.IsCancellationRequested)
      {
        if (await Task.Run(() => Tms.StopEventSignalDuringWait(_stopEventHandle, 1000), stoppingToken))
        {
          Tms.PrintMessage("Получено сообщение об остановке со стороны сервера");
          _applicationLifetime.StopApplication();
          break;
        }
      }
    }


    public override async Task StopAsync(CancellationToken cancellationToken)
    {
      _infr.TerminateTm();

      Tms.TerminateWithoutSql(_tmCid);

      Tms.PrintMessage("Задача будет закрыта");

      await base.StopAsync(cancellationToken);
    }
  }
}