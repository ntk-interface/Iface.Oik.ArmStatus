using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Iface.Oik.Tm.Interfaces;

namespace Iface.Oik.ArmStatus
{
  public class WorkerCache
  {
    private const int CacheSecondsLifetime = 5;


    private readonly ICfsApi _cfsApi;

    private readonly List<TmServer> _tmServersCache         = new List<TmServer>();
    private          DateTime       _tmServersCacheLastPoll = DateTime.UnixEpoch;


    public WorkerCache(ICfsApi cfsApi)
    {
      _cfsApi = cfsApi;
    }


    public IReadOnlyCollection<TmServer> GetTmServers()
    {
      lock (_tmServersCache)
      {
        if (DateTime.Now > _tmServersCacheLastPoll.AddSeconds(CacheSecondsLifetime))
        {
          _tmServersCache.Clear();
          _tmServersCache.AddRange(_cfsApi.GetTmServersTree().ConfigureAwait(false).GetAwaiter().GetResult());
          _tmServersCacheLastPoll = DateTime.Now;
        }
        
        return new List<TmServer>(_tmServersCache);
      }
    }
  }
}