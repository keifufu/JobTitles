namespace JobTitles.Services;

public interface IEventService : IHostedService;

public class EventService(
  ILogger _logger,
  IJobService _jobService,
  IClientState _clientState,
  IObjectTable _objectTable,
  PromptWindow _promptWindow,
  ITitleService _titleService,
  Configuration _configuration) : IEventService
{
  public Task StartAsync(CancellationToken cancellationToken)
  {
    _clientState.Login += OnLogin;
    _clientState.ClassJobChanged += OnJobChanged;
    _clientState.EnterPvP += OnEnterPvP;

    return _logger.ServiceLifecycle();
  }

  public Task StopAsync(CancellationToken cancellationToken)
  {
    _clientState.Login -= OnLogin;
    _clientState.ClassJobChanged -= OnJobChanged;
    _clientState.EnterPvP -= OnEnterPvP;

    return _logger.ServiceLifecycle();
  }

  private void OnLogin()
  {
    _titleService.GetAndCacheCurrentTitleId();
    _jobService.JobChanged(_jobService.GetCurrentJob());
  }

  // `OnJobChanged` triggers twice after logging in, before `LocalPlayer` is set.
  private void OnJobChanged(uint jobId)
  {
    if (_objectTable.LocalPlayer == null)
    {
      _logger.Debug("LocalPlayer is null. Not setting title.");
      return;
    }

    JobService.Job job = JobService.ToJob(jobId);
    _logger.Debug($"Job changed. job::{job} jobId::{jobId}");
    _jobService.JobChanged(job);

    (bool success, string _) = _titleService.SetTitle(job);
    if (success) _promptWindow.Close();
  }

  private void OnEnterPvP()
  {
    if (!_clientState.IsPvPExcludingDen) return;

    CharacterConfig characterConfig = _configuration.GetCharacterConfig();
    if (!characterConfig.UseGAROTitleInPvP) return;

    _logger.Debug("Entered PvP duty");

    ushort pvpTitleId = _titleService.GetPvPTitleId(_jobService.GetCurrentJob());
    if (pvpTitleId is TitleService.TitleIds.None or TitleService.TitleIds.DoNotOverride) return;
    if (pvpTitleId == _titleService.GetAndCacheCurrentTitleId()) return;

    _promptWindow.Open(pvpTitleId);
  }
}
