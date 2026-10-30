using System;using System.Collections.Generic;using System.Runtime.InteropServices;using System.Threading;using System.Globalization;using LibreHardwareMonitor.Hardware;
namespace JonsboStudio {
 sealed class SensorSnapshot {public long version;public Dictionary<string,string> values=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);public string Get(string type,string fallback){string value;return values.TryGetValue(type,out value)?value:fallback;}}
 sealed class CpuSensorReader {
  readonly object gate=new object();Computer computer;IHardware cpu;DateTime sampled=DateTime.MinValue;Dictionary<string,string> values=new Dictionary<string,string>();
  public CpuSensorReader(){try{computer=new Computer{IsCpuEnabled=true};computer.Open();foreach(var item in computer.Hardware)if(item.HardwareType==HardwareType.Cpu){cpu=item;break;}}catch{cpu=null;try{if(computer!=null)computer.Close();}catch{}}}
  public void CopyTo(Dictionary<string,string> target){lock(gate){if(cpu==null)return;if((DateTime.UtcNow-sampled).TotalMilliseconds>=900){sampled=DateTime.UtcNow;try{cpu.Update();var next=new Dictionary<string,string>();double clocks=0;int clockCount=0;foreach(var sensor in cpu.Sensors){if(!sensor.Value.HasValue)continue;double value=sensor.Value.Value;if(sensor.SensorType==SensorType.Temperature&&sensor.Name.StartsWith("CPU Package",StringComparison.Ordinal))next["CPUTemp"]=value.ToString("0",CultureInfo.InvariantCulture);else if(sensor.SensorType==SensorType.Power&&sensor.Name.StartsWith("CPU Package",StringComparison.Ordinal))next["CpuTEC"]=value.ToString("0",CultureInfo.InvariantCulture);else if(sensor.SensorType==SensorType.Clock&&sensor.Name.StartsWith("P-Core #",StringComparison.Ordinal)){clocks+=value;clockCount++;}}if(clockCount>0)next["CpuFrequency"]=(clocks/clockCount).ToString("0",CultureInfo.InvariantCulture);values=next;}catch{values=new Dictionary<string,string>();}}foreach(var item in values)target[item.Key]=item.Value;}}
 }
 sealed class GpuSensorReader {
  readonly object gate=new object();Computer computer;IHardware gpu;DateTime sampled=DateTime.MinValue;Dictionary<string,string> values=new Dictionary<string,string>();
  public GpuSensorReader(){try{computer=new Computer{IsGpuEnabled=true};computer.Open();foreach(var item in computer.Hardware)if(item.HardwareType.ToString().StartsWith("Gpu",StringComparison.OrdinalIgnoreCase)){gpu=item;break;}}catch{gpu=null;try{if(computer!=null)computer.Close();}catch{}}}
  public void CopyTo(Dictionary<string,string> target){lock(gate){if(gpu==null)return;if((DateTime.UtcNow-sampled).TotalMilliseconds>=900){sampled=DateTime.UtcNow;try{gpu.Update();var next=new Dictionary<string,string>();foreach(var sensor in gpu.Sensors){if(!sensor.Value.HasValue)continue;double value=sensor.Value.Value;if(sensor.SensorType==SensorType.Temperature&&(!next.ContainsKey("GPUTemp")||sensor.Name.IndexOf("Core",StringComparison.OrdinalIgnoreCase)>=0))next["GPUTemp"]=value.ToString("0",CultureInfo.InvariantCulture);else if(sensor.SensorType==SensorType.Load&&sensor.Name.IndexOf("Core",StringComparison.OrdinalIgnoreCase)>=0)next["GpuUsage"]=value.ToString("0",CultureInfo.InvariantCulture);else if(sensor.SensorType==SensorType.Clock&&sensor.Name.IndexOf("Core",StringComparison.OrdinalIgnoreCase)>=0)next["GpuFrequency"]=value.ToString("0",CultureInfo.InvariantCulture);else if(sensor.SensorType==SensorType.Power&&(!next.ContainsKey("GpuTEC")||sensor.Name.IndexOf("Total",StringComparison.OrdinalIgnoreCase)>=0))next["GpuTEC"]=value.ToString("0",CultureInfo.InvariantCulture);}values=next;}catch{values=new Dictionary<string,string>();}}foreach(var item in values)if(!target.ContainsKey(item.Key))target[item.Key]=item.Value;}}
 }
 sealed class SensorSampler:IDisposable {
  static readonly Lazy<CpuSensorReader> cpuReader=new Lazy<CpuSensorReader>(()=>new CpuSensorReader());
  static readonly Lazy<GpuSensorReader> gpuReader=new Lazy<GpuSensorReader>(()=>new GpuSensorReader());
  System.Threading.Timer timer;ulong idleBefore,totalBefore;long version;IntPtr gpu;bool nvml,nvmlInitialized;public volatile SensorSnapshot Latest=new SensorSnapshot();
  [StructLayout(LayoutKind.Sequential)]class MemoryStatus{public uint length=64,load;public ulong total,available,pageTotal,pageAvailable,virtualTotal,virtualAvailable,extended;}
  [StructLayout(LayoutKind.Sequential)]struct GpuUtil{public uint gpu,memory;}
  [DllImport("kernel32.dll")]static extern bool GetSystemTimes(out ulong idle,out ulong kernel,out ulong user);
  [DllImport("kernel32.dll")]static extern bool GlobalMemoryStatusEx(MemoryStatus status);
  [DllImport("nvml.dll",CallingConvention=CallingConvention.Cdecl)]static extern int nvmlInit_v2();
  [DllImport("nvml.dll",CallingConvention=CallingConvention.Cdecl)]static extern int nvmlShutdown();
  [DllImport("nvml.dll",CallingConvention=CallingConvention.Cdecl)]static extern int nvmlDeviceGetHandleByIndex_v2(uint index,out IntPtr handle);
  [DllImport("nvml.dll",CallingConvention=CallingConvention.Cdecl)]static extern int nvmlDeviceGetTemperature(IntPtr handle,uint sensor,out uint value);
  [DllImport("nvml.dll",CallingConvention=CallingConvention.Cdecl)]static extern int nvmlDeviceGetUtilizationRates(IntPtr handle,out GpuUtil value);
  [DllImport("nvml.dll",CallingConvention=CallingConvention.Cdecl)]static extern int nvmlDeviceGetClockInfo(IntPtr handle,uint clock,out uint value);
  [DllImport("nvml.dll",CallingConvention=CallingConvention.Cdecl)]static extern int nvmlDeviceGetPowerUsage(IntPtr handle,out uint value);
  public SensorSampler(){try{nvmlInitialized=nvmlInit_v2()==0;if(nvmlInitialized)nvml=nvmlDeviceGetHandleByIndex_v2(0,out gpu)==0;}catch{}Sample(null);timer=new System.Threading.Timer(Sample,null,1000,1000);}
  void Sample(object ignored){var next=new SensorSnapshot{version=Interlocked.Increment(ref version)};var d=next.values;ulong idle,kernel,user;if(GetSystemTimes(out idle,out kernel,out user)){ulong total=kernel+user;if(totalBefore>0&&total>totalBefore)d["CpuUsage"]=(100*(1-(idle-idleBefore)/(double)(total-totalBefore))).ToString("0",CultureInfo.InvariantCulture);idleBefore=idle;totalBefore=total;}var memory=new MemoryStatus();if(GlobalMemoryStatusEx(memory)){d["MemoryUsage"]=memory.load.ToString();d["UsedMemory"]=((memory.total-memory.available)/1073741824.0).ToString("0.0",CultureInfo.InvariantCulture);d["TotalMemory"]=(memory.total/1073741824.0).ToString("0.0",CultureInfo.InvariantCulture);d["RAMUsage"]=d["MemoryUsage"];}
   d["CurrentTime"]=DateTime.Now.ToString("HH:mm:ss");d["CurrentDates"]=DateTime.Now.ToString("yyyy-MM-dd");d["WeekDaysL3"]=DateTime.Now.ToString("ddd");d["CPUTemp"]="—";d["CpuFrequency"]="—";d["CpuTEC"]="—";cpuReader.Value.CopyTo(d);
   if(nvml)try{uint value;GpuUtil util;if(nvmlDeviceGetTemperature(gpu,0,out value)==0)d["GPUTemp"]=value.ToString();if(nvmlDeviceGetUtilizationRates(gpu,out util)==0)d["GpuUsage"]=util.gpu.ToString();if(nvmlDeviceGetClockInfo(gpu,0,out value)==0)d["GpuFrequency"]=value.ToString();if(nvmlDeviceGetPowerUsage(gpu,out value)==0)d["GpuTEC"]=(value/1000.0).ToString("0",CultureInfo.InvariantCulture);}catch{}
   if(!d.ContainsKey("GPUTemp"))gpuReader.Value.CopyTo(d);
   Latest=next;
  }
  public void Dispose(){if(timer!=null)timer.Dispose();if(nvmlInitialized)try{nvmlShutdown();}catch{}}
 }
}
