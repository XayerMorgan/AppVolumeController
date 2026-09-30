using System.IO;
using NAudio.Wave;
using AppVolumeController.Models;
using AppVolumeController.Services;
namespace AppVolumeController;
internal static class SelfTest
{
 private sealed class Silence : IWaveProvider {public WaveFormat WaveFormat {get;}=new(44100,16,2);public int Read(byte[] buffer,int offset,int count){Array.Clear(buffer,offset,count);return count;}}
 public static int Run()
 {
  var log=new List<string>(); var path=Path.Combine(Path.GetTempPath(),"appvolume-test-"+Guid.NewGuid()+".json");
  try
  {
   void Assert(bool ok,string message){if(!ok)throw new Exception(message);log.Add("PASS: "+message);}
   var prefs=new Preferences();prefs.For("discord").Volume=37;prefs.RememberVolumes=true;prefs.Save(path);
   var loaded=Preferences.Load(path);Assert(loaded.For("DISCORD").Volume==37 && loaded.RememberVolumes,"Preferences round trip and case insensitive keys");
   File.WriteAllText(path,"invalid json");Assert(!Preferences.Load(path).RememberVolumes,"Corrupt preferences recover safely");
   Assert(AudioService.FriendlyName("blackdesert64","fallback")=="Black Desert Online","BDO recognition");
   using var output=new WasapiOut();output.Init(new Silence());output.Play();Thread.Sleep(350);
   using var audio=new AudioService();audio.Scan();
   Assert(audio.Master!=null,"Default output detected");
   var session=audio.Sessions.FirstOrDefault(s=>s.Control.GetProcessID==Environment.ProcessId) ?? throw new Exception("Test audio session not discovered");
   var volume=session.Control.SimpleAudioVolume;var old=volume.Volume;var mute=volume.Mute;
   try {volume.Volume=0.37f;Assert(Math.Abs(volume.Volume-0.37f)<0.01,"Core Audio session volume write/read");volume.Mute=true;Assert(volume.Mute,"Core Audio session mute write/read");volume.Mute=false;Assert(!volume.Mute,"Core Audio session unmute");}
   finally {volume.Volume=old;volume.Mute=mute;}
   audio.Scan();Assert(audio.Sessions.Any(s=>s.Control.GetProcessID==Environment.ProcessId),"Rescan reconnects sessions");
   output.Stop();audio.Scan();log.Add("PASS: Stopped audio session rescan");
   File.WriteAllLines(Path.Combine(AppContext.BaseDirectory,"self-test.log"),log);return 0;
  }
  catch(Exception e){log.Add("FAIL: "+e);File.WriteAllLines(Path.Combine(AppContext.BaseDirectory,"self-test.log"),log);return 1;}
  finally {if(File.Exists(path))File.Delete(path);}
 }
}
