namespace JobTitles.Services;

public interface IJobService
{
  JobService.Job GetCurrentJob();
  void JobChanged(JobService.Job job);
  bool IsClass(JobService.Job job);
  JobService.Job GetJobFromClass(JobService.Job job);
}

public class JobService(IObjectTable _objectTable) : IJobService
{
  private Job _lastJob = Job.ADV;

  public enum Job : uint
  {
    ADV, GLD, PUG, MRD, LNC, ARC, CNJ, THM, CRP, BSM, ARM, GSM, LTW, WVR, ALC, CUL, MIN, BOT, FSH,
    PLD, MNK, WAR, DRG, BRD, WHM, BLM, ACN, SMN, SCH, ROG, NIN, MCH, DRK, AST, SAM, RDM, BLU, GNB,
    DNC, RPR, SGE, VPR, PCT, BST
  };

  public static readonly HashSet<Job> Tanks = [Job.PLD, Job.WAR, Job.DRK, Job.GNB, Job.GLD, Job.MRD];
  public static readonly HashSet<Job> Healers = [Job.WHM, Job.SCH, Job.AST, Job.SGE, Job.CNJ];
  public static readonly HashSet<Job> Melee = [Job.MNK, Job.DRG, Job.NIN, Job.SAM, Job.RPR, Job.VPR, Job.PUG, Job.LNC, Job.ROG, Job.BST];
  public static readonly HashSet<Job> Ranged = [Job.BRD, Job.MCH, Job.DNC, Job.BLM, Job.SMN, Job.RDM, Job.PCT, Job.BLU, Job.ARC, Job.THM, Job.ACN];
  public static readonly HashSet<Job> Crafters = [Job.CRP, Job.BSM, Job.ARM, Job.GSM, Job.LTW, Job.WVR, Job.ALC, Job.CUL];
  public static readonly HashSet<Job> Gatherers = [Job.MIN, Job.BOT, Job.FSH];

  private static readonly Dictionary<Job, Job> ClassJobMap = new()
  {
    { Job.GLD, Job.PLD },
    { Job.PUG, Job.MNK },
    { Job.MRD, Job.WAR },
    { Job.LNC, Job.DRG },
    { Job.ARC, Job.BRD },
    { Job.CNJ, Job.WHM },
    { Job.THM, Job.BLM },
    { Job.ACN, Job.SMN },
    { Job.ROG, Job.NIN },
  };

  public static IEnumerable<Job> AllJobs =>
    Tanks.Concat(Healers).Concat(Melee).Concat(Ranged).Concat(Crafters).Concat(Gatherers);

  public static Job ToJob(uint jobId) => Enum.IsDefined(typeof(Job), jobId) ? (Job)jobId : Job.ADV;

  public Job GetCurrentJob()
  {
    if (_objectTable.LocalPlayer == null) return _lastJob;
    _lastJob = ToJob(_objectTable.LocalPlayer.ClassJob.RowId);
    return _lastJob;
  }

  public void JobChanged(Job job)
    => _lastJob = job;

  public bool IsClass(Job job) => ClassJobMap.ContainsKey(job);

  public Job GetJobFromClass(Job job) => ClassJobMap.GetValueOrDefault(job, Job.ADV);
}
