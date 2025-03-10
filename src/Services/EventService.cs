namespace JobTitles.Services;

public class EventService : IHostedService
{
  private readonly Logger Logger;
  private readonly Configuration Configuration;
  private readonly JobService JobService;
  private readonly TitleService TitleService;
  private readonly PromptWindow PromptWindow;
  private readonly IClientState ClientState;

  public EventService(Logger logger, Configuration configuration, JobService jobService, TitleService titleService, PromptWindow promptWindow, IClientState clientState)
  {
    Logger = logger;
    Configuration = configuration;
    JobService = jobService;
    TitleService = titleService;
    PromptWindow = promptWindow;
    ClientState = clientState;
  }

  public Task StartAsync(CancellationToken cancellationToken)
  {
    ClientState.Login += OnLogin;
    ClientState.ClassJobChanged += OnJobChanged;
    ClientState.EnterPvP += OnEnterPvP;

    Logger.Debug("EventService started");
    return Task.CompletedTask;
  }

  public Task StopAsync(CancellationToken cancellationToken)
  {
    ClientState.Login -= OnLogin;
    ClientState.ClassJobChanged -= OnJobChanged;
    ClientState.EnterPvP -= OnEnterPvP;

    Logger.Debug("EventService stopped");
    return Task.CompletedTask;
  }

  // If `OnLogin` reverts to invoking without checking for `LocalPlayer` existing,
  // then move this to `Framework.OnUpdate`.
  private void OnLogin()
  {
    TitleService.GetAndCacheCurrentTitleId();
    JobService.OnJobChanged(JobService.GetCurrentJob());
  }

  // `OnJobChanged` triggers twice after logging in, before `LocalPlayer` is set.
  private void OnJobChanged(uint jobId)
  {
    if (ClientState.LocalPlayer == null)
    {
      Logger.Debug("LocalPlayer is null. Not setting title.");
      return;
    }

    JobService.Job job = JobService.ToJob(jobId);
    Logger.Debug($"Job changed. job::{job} jobId::{jobId}");
    JobService.OnJobChanged(job);

    (bool success, string title) = TitleService.SetTitle(job);
    if (success) PromptWindow.Close();
  }

  private void OnEnterPvP()
  {
    if (!ClientState.IsPvPExcludingDen) return;

    CharacterConfig characterConfig = Configuration.GetCharacterConfig();
    if (!characterConfig.UseGAROTitleInPvP) return;

    Logger.Debug("Entered PvP duty");

    ushort pvpTitleId = TitleService.GetPvPTitleId(JobService.GetCurrentJob());
    if (pvpTitleId == TitleService.TitleIds.None || pvpTitleId == TitleService.TitleIds.DoNotOverride) return;
    if (pvpTitleId == TitleService.GetAndCacheCurrentTitleId()) return;

    PromptWindow.Open(pvpTitleId);
  }
}
