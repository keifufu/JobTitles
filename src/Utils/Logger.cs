using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace JobTitles.Utils;

public static class Logger
{

  private static readonly Dictionary<string, DateTime> _lastLogTime = new();
  private static readonly TimeSpan _throttleInterval = TimeSpan.FromSeconds(15);

  private enum LogType
  {
    Error,
    Debug,
  }

  private static void Log(LogType type, string text, string functionName, params object[] values)
  {
    string formattedText = $"[{functionName}] {text}";

    bool ShouldLog(string message)
    {
      if (_lastLogTime.TryGetValue(message, out var lastLogTime))
      {
        if (DateTime.UtcNow - lastLogTime < _throttleInterval)
        {
          return false;
        }
      }

      return true;
    }

    if (type == LogType.Error)
    {
      if (ShouldLog(formattedText))
      {
        Plugin.Log.Error(formattedText, values);
        _lastLogTime[formattedText] = DateTime.UtcNow;
      }
    }
    else if (type == LogType.Debug && Plugin.Configuration.Debug)
    {
      if (ShouldLog(formattedText))
      {
        Plugin.Log.Debug(formattedText, values);
        _lastLogTime[formattedText] = DateTime.UtcNow;
      }
    }
  }

  public static void Error(string text, [CallerMemberName] string? functionName = null, params object[] values) =>
    Log(LogType.Error, text, functionName ?? "UnknownFunction", values);

  public static void Debug(string text, [CallerMemberName] string? functionName = null, params object[] values) =>
    Log(LogType.Debug, text, functionName ?? "UnknownFunction", values);
}
