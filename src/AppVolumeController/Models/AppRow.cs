using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using AppVolumeController.Services;
namespace AppVolumeController.Models;
public sealed class AppRow : INotifyPropertyChanged
{
 public string Key {get;init;}=""; public string Name {get;init;}=""; public ImageSource? Icon {get;init;}
 public List<SessionInfo> Sessions {get;set;} = new();
 public Action<AppRow,string>? Changed {get;set;}
 private int volume; private bool muted,favorite; private string detail="";
 public int Volume {get=>volume;set {var clamped=Math.Clamp(value,0,100);if(volume==clamped)return;volume=clamped;Notify();Changed?.Invoke(this,"volume");}}
 public bool Muted {get=>muted;set {if(muted==value)return;muted=value;Notify();Changed?.Invoke(this,"mute");}}
 public bool Favorite {get=>favorite;set {if(favorite==value)return;favorite=value;Notify();Changed?.Invoke(this,"favorite");}}
 public string Detail {get=>detail;set {if(detail==value)return;detail=value;Notify();}}
 public void Sync(int v,bool m) {v=Math.Clamp(v,0,100);if(volume!=v){volume=v;Notify(nameof(Volume));}if(muted!=m){muted=m;Notify(nameof(Muted));}}
 public event PropertyChangedEventHandler? PropertyChanged;
 private void Notify([CallerMemberName]string? n=null)=>PropertyChanged?.Invoke(this,new(n));
}
