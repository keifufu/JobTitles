using System;
using System.Collections.Generic;
using System.Linq;

namespace JobTitles.Utils;

public static class JobUtils
{
  public enum Job : uint
  {
    ADV, GLD, PUG, MRD, LNC, ARC, CNJ, THM, CRP, BSM, ARM, GSM, LTW, WVR, ALC, CUL, MIN, BOT, FSH,
    PLD, MNK, WAR, DRG, BRD, WHM, BLM, ACN, SMN, SCH, ROG, NIN, MCH, DRK, AST, SAM, RDM, BLU, GNB,
    DNC, RPR, SGE, VPR, PCT
  };

  public static readonly Job[] Tanks = { Job.PLD, Job.WAR, Job.DRK, Job.GNB, Job.GLD, Job.MRD };
  public static readonly Job[] Healers = { Job.WHM, Job.SCH, Job.AST, Job.SGE, Job.CNJ };
  public static readonly Job[] Melee = { Job.MNK, Job.DRG, Job.NIN, Job.SAM, Job.RPR, Job.VPR, Job.PUG, Job.LNC, Job.ROG };
  public static readonly Job[] Ranged = { Job.BRD, Job.MCH, Job.DNC, Job.BLM, Job.SMN, Job.RDM, Job.PCT, Job.BLU, Job.ARC, Job.THM, Job.ACN };
  public static readonly Job[] Crafters = { Job.CRP, Job.BSM, Job.ARM, Job.GSM, Job.LTW, Job.WVR, Job.ALC, Job.CUL };
  public static readonly Job[] Gatherers = { Job.MIN, Job.BOT, Job.FSH };

  public static Job[] OrderedJobs =
    Tanks
    .Concat(Healers)
    .Concat(Melee)
    .Concat(Ranged)
    .Concat(Crafters)
    .Concat(Gatherers)
    .ToArray();

  public static readonly Job[] Classes = { Job.GLD, Job.PUG, Job.MRD, Job.LNC, Job.ARC, Job.CNJ, Job.THM, Job.ACN, Job.ROG };

  public static bool IsClass(uint jobId) =>
    Enum.IsDefined(typeof(Job), jobId) && Classes.Contains((Job)jobId);

  public static readonly Dictionary<Job, Job> JobClassMap = new()
  {
    { Job.GLD, Job.PLD },
    { Job.MRD, Job.WAR },
    { Job.PUG, Job.MNK },
    { Job.LNC, Job.DRG },
    { Job.ROG, Job.NIN },
    { Job.ARC, Job.BRD },
    { Job.THM, Job.BLM },
    { Job.ACN, Job.SMN },
  };

  public static uint GetJobIdForClassId(uint classId) =>
    Enum.IsDefined(typeof(Job), classId) && JobClassMap.TryGetValue((Job)classId, out var job) ? (uint)job : (uint)Job.ADV;
}
