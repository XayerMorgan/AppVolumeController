using System.IO;
using System.Text.Json;
namespace AppVolumeController.Models;
public sealed class AppPreference { public bool Favorite {get;set;} public int? Volume {get;set;} public bool? Muted {get;set;} }
public sealed class Preferences
{
 public bool RememberVolumes {get;set;} = false;
 public AppearancePreference Appearance {get;set;} = new();
 public Dictionary<string,AppPreference> Apps {get;set;} = new(StringComparer.OrdinalIgnoreCase);
 public static string FilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"AppVolumeController","preferences.json");
 public static Preferences Load(string? path = null)
 {
  try { var p = JsonSerializer.Deserialize<Preferences>(File.ReadAllText(path ?? FilePath)) ?? new(); p.Apps = new(p.Apps ?? new(), StringComparer.OrdinalIgnoreCase); p.Appearance ??= new(); p.Appearance.Normalize(); return p; }
  catch(Exception e) when(e is IOException or JsonException or UnauthorizedAccessException) { return new(); }
 }
 public void Save(string? path = null)
 {
  path ??= FilePath; Directory.CreateDirectory(Path.GetDirectoryName(path)!);
  File.WriteAllText(path+".tmp",JsonSerializer.Serialize(this,new JsonSerializerOptions {WriteIndented=true})); File.Move(path+".tmp",path,true);
 }
 public AppPreference For(string key) { if(!Apps.TryGetValue(key,out var p)) Apps[key] = p = new() {Favorite=key is "blackdesert64" or "blackdesert" or "discord"}; return p; }
}
