using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Newtonsoft.Json.Linq;

namespace JonsboStudio {
 // CPU Set IDs are discovered at runtime; logical processor numbers are not portable.
 static class CpuScheduling {
  internal sealed class Core {public uint id;public byte efficiency;}
  static readonly string path=System.IO.Path.Combine(AppPaths.Data("Settings"),"performance.json");
  public static bool Enabled{get;private set;}
  static string status="Windows manages processor placement.";public static string Status{get{return status;}private set{status=value;}}
  [DllImport("kernel32.dll",SetLastError=true)]static extern bool GetSystemCpuSetInformation(IntPtr data,uint length,out uint returned,IntPtr process,uint flags);
  [DllImport("kernel32.dll",SetLastError=true)]static extern bool SetProcessDefaultCpuSets(IntPtr process,uint[] ids,uint count);
  [DllImport("kernel32.dll")]static extern IntPtr GetCurrentProcess();
  internal static uint[] Select(IEnumerable<Core> topology){var cores=topology.ToArray();if(cores.Length==0||cores.Select(c=>c.efficiency).Distinct().Count()<2)return new uint[0];byte lowest=cores.Min(c=>c.efficiency);return cores.Where(c=>c.efficiency==lowest).Select(c=>c.id).Distinct().ToArray();}
  internal static Core[] Discover(){uint length;IntPtr process=GetCurrentProcess();GetSystemCpuSetInformation(IntPtr.Zero,0,out length,process,0);if(length==0)return new Core[0];IntPtr buffer=Marshal.AllocHGlobal(checked((int)length));try{uint returned;if(!GetSystemCpuSetInformation(buffer,length,out returned,process,0))throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());var cores=new List<Core>();for(int offset=0;offset+8<=returned;){IntPtr item=IntPtr.Add(buffer,offset);int size=Marshal.ReadInt32(item);if(size<8||size>returned-offset)throw new InvalidOperationException("Invalid Windows CPU topology.");if(Marshal.ReadInt32(item,4)==0&&size>=24){byte flags=Marshal.ReadByte(item,19);if((flags&2)==0||(flags&4)!=0)cores.Add(new Core{id=unchecked((uint)Marshal.ReadInt32(item,8)),efficiency=Marshal.ReadByte(item,18)});}offset+=size;}return cores.ToArray();}finally{Marshal.FreeHGlobal(buffer);}}
  public static void Initialize(){try{if(System.IO.File.Exists(path))Set((bool?)JObject.Parse(System.IO.File.ReadAllText(path))["efficiencyCores"]==true,false);}catch{Enabled=false;Status="Windows manages processor placement.";}}
  public static void Set(bool enabled,bool persist=true){try{uint[] ids=enabled?Select(Discover()):new uint[0];if(enabled&&ids.Length==0){Enabled=false;Status="This processor does not expose separate efficiency cores.";if(!SetProcessDefaultCpuSets(GetCurrentProcess(),null,0))throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());}else{if(!SetProcessDefaultCpuSets(GetCurrentProcess(),ids.Length==0?null:ids,(uint)ids.Length))throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());Enabled=enabled;Status=enabled?"Playback uses "+ids.Length+" efficiency processors. Turn off if playback slows.":"Windows manages processor placement.";}}catch(EntryPointNotFoundException){Enabled=false;Status="CPU scheduling preferences require Windows 10 or later.";}if(persist)AppPaths.SaveJson(path,new JObject{{"efficiencyCores",Enabled}});}
  public static void SelfTest(){if(Select(new[]{new Core{id=1,efficiency=0},new Core{id=2,efficiency=0}}).Length!=0)throw new Exception("Homogeneous CPU must use Windows scheduling.");if(!Select(new[]{new Core{id=20,efficiency=1},new Core{id=8,efficiency=0},new Core{id=9,efficiency=0}}).SequenceEqual(new uint[]{8,9}))throw new Exception("Efficiency CPU selection failed.");var cores=Discover();Console.WriteLine("CPU topology: "+cores.Length+" logical processors; "+Select(cores).Length+" efficiency processors; classes "+string.Join(",",cores.Select(c=>c.efficiency).Distinct()));bool old=Enabled;Set(true,false);Console.WriteLine(Status);Set(old,false);Console.WriteLine("PASS: efficiency-core discovery, selection, assignment and reset.");}
 }
}
