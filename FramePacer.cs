using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;
namespace JonsboStudio {
 // Windows 11 may stop honoring timeBeginPeriod for an occluded window.
 // An explicit high-resolution timer keeps USB deadlines independent of visibility.
 sealed class FramePacer:IDisposable {
  readonly WaitHandle[] handles;readonly EventWaitHandle timer;readonly WaitHandle stop;
  [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)]static extern IntPtr CreateWaitableTimerEx(IntPtr attributes,string name,uint flags,uint access);
  [DllImport("kernel32.dll",SetLastError=true)]static extern bool SetWaitableTimer(IntPtr handle,ref long due,int period,IntPtr callback,IntPtr context,bool resume);
  public FramePacer(WaitHandle stop){this.stop=stop;try{IntPtr handle=CreateWaitableTimerEx(IntPtr.Zero,null,2,0x1f0003);if(handle!=IntPtr.Zero){timer=new EventWaitHandle(false,EventResetMode.AutoReset);timer.SafeWaitHandle=new SafeWaitHandle(handle,true);handles=new WaitHandle[]{stop,timer};}}catch(EntryPointNotFoundException){}}
  public bool Wait(double milliseconds){if(milliseconds<=0)return stop.WaitOne(0);if(timer==null)return stop.WaitOne(Math.Max(1,(int)Math.Ceiling(milliseconds)));long due=-(long)Math.Ceiling(milliseconds*10000);if(!SetWaitableTimer(timer.SafeWaitHandle.DangerousGetHandle(),ref due,0,IntPtr.Zero,IntPtr.Zero,false))throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());return WaitHandle.WaitAny(handles)==0;}
  public void Dispose(){if(timer!=null)timer.Dispose();}
 }
}
