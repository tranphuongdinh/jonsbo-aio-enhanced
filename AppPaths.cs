using System;using System.IO;using Newtonsoft.Json.Linq;
namespace JonsboStudio {
 static class AppPaths {
  public static readonly string Root=AppDomain.CurrentDomain.BaseDirectory;
  public static readonly string Installed=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"JONSBO-AIO");
  public static string Data(string name){string path=Path.Combine(Root,"Data",name);Directory.CreateDirectory(path);return path;}
  public static string AssetUrl(string path){if(string.IsNullOrEmpty(path))return "";string full=Path.GetFullPath(path),root=Path.GetFullPath(Root),installed=Path.GetFullPath(Installed);if(full.StartsWith(root,StringComparison.OrdinalIgnoreCase))return "https://studio.local/"+Uri.EscapeUriString(full.Substring(root.Length).Replace('\\','/'));if(full.StartsWith(installed+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))return "https://installed.local/"+Uri.EscapeUriString(full.Substring(installed.Length+1).Replace('\\','/'));return "";}
  public static void SaveJson(string path,JToken value){Directory.CreateDirectory(Path.GetDirectoryName(path));string temp=path+".tmp";File.WriteAllText(temp,value.ToString(Newtonsoft.Json.Formatting.Indented));if(File.Exists(path))File.Replace(temp,path,path+".bak");else File.Move(temp,path);}
 }
 sealed class StudioSettings {
  public string ThemePath="",Port="";public int Width=1920,Height=462,Fps=60,Quality=80,CacheMB=128,Rotation=-1;public bool Resume=true,WasPlaying=false;
  public static string FilePath{get{return Path.Combine(AppPaths.Data("Settings"),"session.json");}}
  public static StudioSettings Load(){try{return Newtonsoft.Json.JsonConvert.DeserializeObject<StudioSettings>(File.ReadAllText(FilePath))??new StudioSettings();}catch{return new StudioSettings();}}
  public void Save(){AppPaths.SaveJson(FilePath,JObject.FromObject(this));}
 }
}
