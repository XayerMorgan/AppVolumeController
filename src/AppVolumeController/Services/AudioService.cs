using System.Diagnostics;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
namespace AppVolumeController.Services;
public sealed record SessionInfo(string Id,string Key,string Name,string? Path,AudioSessionControl Control);
public sealed class AudioService : IDisposable
{
 private readonly MMDeviceEnumerator enumerator = new();
 private readonly List<MMDevice> devices = new();
 public List<SessionInfo> Sessions {get;} = new();
 public MMDevice? Master {get;private set;}
 public string? Error {get;private set;}
 public static string FriendlyName(string key,string fallback) => key.ToLowerInvariant() switch { "blackdesert64" or "blackdesert" => "Black Desert Online", "discord" => "Discord", "system" => "System sounds", _ => fallback };
 public void Scan()
 {
  Clear(); Error=null;
  try { Master=enumerator.GetDefaultAudioEndpoint(DataFlow.Render,Role.Multimedia); } catch(Exception e) { Error=e.Message; }
  try
  {
   foreach(var device in enumerator.EnumerateAudioEndPoints(DataFlow.Render,DeviceState.Active))
   {
    devices.Add(device);
    try
    {
     var sessions=device.AudioSessionManager.Sessions;
     for(int i=0;i<sessions.Count;i++)
     {
      var session=sessions[i];
      try
      {
       if(session.State==AudioSessionState.AudioSessionStateExpired) {session.Dispose();continue;}
       string key="system", name="System sounds"; string? path=null;
       if(!session.IsSystemSoundsSession)
       {
        var pid=(int)session.GetProcessID; key="process-"+pid; name="Application "+pid;
        try { using var process=Process.GetProcessById(pid); key=process.ProcessName.ToLowerInvariant(); name=process.ProcessName; try {path=process.MainModule?.FileName; } catch {} } catch {}
       }
       Sessions.Add(new(device.ID+"|"+session.GetSessionInstanceIdentifier,key,FriendlyName(key,name),path,session));
      } catch {session.Dispose();}
     }
    } catch(Exception e) {Error=e.Message;}
   }
  } catch(Exception e) {Error=e.Message;}
 }
 private void Clear() {foreach(var s in Sessions) s.Control.Dispose();Sessions.Clear();foreach(var d in devices)d.Dispose();devices.Clear();Master?.Dispose();Master=null;}
 public void Dispose() {Clear();enumerator.Dispose();}
}
