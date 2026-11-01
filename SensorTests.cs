using System;using System.Collections.Generic;using System.Threading;using Newtonsoft.Json.Linq;using LibreHardwareMonitor.Hardware;
namespace JonsboStudio {
 static class SensorTests {
  static void Check(bool condition,string message){if(!condition)throw new Exception("Sensor test: "+message);}
  public static void Run(){
   var snapshot=new SensorSnapshot();SensorSampler.AddMemory(snapshot.values,32UL*1073741824,20UL*1073741824,38);
   Check(snapshot.Get("MemoryUsageSize","")=="12.0"&&snapshot.Get("MemorySize","")=="32.0"&&snapshot.Get("MemoryUseInt","")=="38","native RAM bindings must resolve without a hardware driver");
   Check(ThemeDocument.BoundValue(new JObject{{"TextType","MemoryUsageSize"},{"Unit"," MB"}},snapshot)=="12288 MB","native memory unit conversion");
   snapshot.values["DownNetSpeed"]="2048";Check(ThemeDocument.BoundValue(new JObject{{"TextType","DownNetSpeed"},{"Unit"," MB/s"}},snapshot)=="2.0 MB/s","network unit conversion");
   var intel=SensorMapping.Cpu(new[]{new SensorReading("CPU Package",SensorType.Temperature,67),new SensorReading("CPU Core #1",SensorType.Temperature,70),new SensorReading("Core #1",SensorType.Clock,4800),new SensorReading("CPU Package",SensorType.Power,55),new SensorReading("CPU Core",SensorType.Voltage,1.2)});
   Check(intel["CPUTemp"]=="67"&&intel["CpuFrequency"]=="4800"&&intel["CpuTEC"]=="55"&&intel["CpuVoltage"]=="1.20","Intel package and non-hybrid core sensors");
   var amd=SensorMapping.Cpu(new[]{new SensorReading("Core (Tdie)",SensorType.Temperature,64),new SensorReading("Core (Tctl)",SensorType.Temperature,74),new SensorReading("Core #1",SensorType.Clock,4200),new SensorReading("Package",SensorType.Power,65)});
   Check(amd["CPUTemp"]=="64"&&amd["CpuTEC"]=="65"&&amd["CpuFrequency"]=="4200","AMD die/core/package names");
   Check(!SensorMapping.Cpu(new[]{new SensorReading("CPU Package",SensorType.Temperature,0)}).ContainsKey("CPUTemp"),"missing driver must not become a fabricated zero temperature");
   using(var sampler=new SensorSampler(true,true)){long first=sampler.Latest.version;Thread.Sleep(1500);var sample=sampler.Latest;Check(sample.version>first,"sensor snapshots continue updating");foreach(string key in new[]{"MemoryUsageSize","MemorySize","MemoryUseInt","DownNetSpeed","UpNetSpeed"})Check(sample.Get(key,"").Length>0,"missing live native counter "+key);Console.WriteLine("Sensor status: "+SensorSetup.Status(sample));Console.WriteLine("Live native sensor values: "+Newtonsoft.Json.JsonConvert.SerializeObject(sample.values));}
   Console.WriteLine("PASS: native RAM/network aliases, Intel/AMD mappings, unit conversion, live updates and unavailable-temperature handling.");
  }
 }
}
