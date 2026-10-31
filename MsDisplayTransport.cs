using System;using System.IO;using System.Drawing;using System.Drawing.Imaging;using System.Runtime.InteropServices;using System.Threading;
namespace JonsboStudio {
 // DS339 uses the bundled MacroSilicon runtime and USB display driver.
 sealed class MsDisplayTransport:IDisplayTransport {
  public const string PortId="MSDISPLAY:345F:9132";
  [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Ansi)]struct Edid {public int mode;[MarshalAs(UnmanagedType.ByValTStr,SizeConst=128)]public string replace;public int count;[MarshalAs(UnmanagedType.ByValArray,SizeConst=192)]public byte[] timings;}
  [StructLayout(LayoutKind.Sequential)]struct Resolution {public int width,height,refresh;}
  [StructLayout(LayoutKind.Sequential)]struct Picture {public int width,height;public IntPtr data;}
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate void AttachHandler(uint handle,IntPtr resolutions,int count);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate void DetachHandler(uint handle);
  [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)]static extern bool SetDllDirectory(string path);
  [DllImport("MSDISPLAYSDKWRRAPER.dll",CallingConvention=CallingConvention.Cdecl,EntryPoint="Wrraper_MSDisplayStart")]static extern int Start(int level,IntPtr edid);
  [DllImport("MSDISPLAYSDKWRRAPER.dll",CallingConvention=CallingConvention.Cdecl,EntryPoint="Wrraper_MSDisplayStop")]static extern int Stop();
  [DllImport("MSDISPLAYSDKWRRAPER.dll",CallingConvention=CallingConvention.Cdecl,EntryPoint="Wrraper_MSDisplayRegisterCallback")]static extern void Register(IntPtr attach,IntPtr detach);
  [DllImport("MSDISPLAYSDKWRRAPER.dll",CallingConvention=CallingConvention.Cdecl,EntryPoint="Wrraper_MSDisplayEnableSDKScreenProcessor")]static extern int Enable(bool on);
  [DllImport("MSDISPLAYSDKWRRAPER.dll",CallingConvention=CallingConvention.Cdecl,EntryPoint="Wrraper_MSDisplayReadSN")]static extern int ReadSN(uint handle,IntPtr data);
  [DllImport("MSDISPLAYSDKWRRAPER.dll",CallingConvention=CallingConvention.Cdecl,EntryPoint="Wrraper_MSDisplaySetVideoParam")]static extern int SetVideoParam(uint handle,IntPtr data);
  [DllImport("MSDISPLAYSDKWRRAPER.dll",CallingConvention=CallingConvention.Cdecl,EntryPoint="Wrraper_MSDisplaySendPicture")]static extern int SendPicture(uint handle,IntPtr data,bool encodeJpeg);
  readonly ManualResetEventSlim attached=new ManualResetEventSlim(false);AttachHandler onAttach;DetachHandler onDetach;uint handle;bool started,disposed;string serialNumber="";Bitmap decoded,frame;Graphics graphics;TurboCodec codec;
  public string SerialNumber{get{return serialNumber;}}public int NativeWidth{get{return 376;}}public int NativeHeight{get{return 960;}}
  public void Connect(string port){if(port!=PortId)throw new ArgumentException("Wrong display interface.");if(System.Diagnostics.Process.GetProcessesByName("JONSBO-AIO").Length>0)throw new InvalidOperationException("Exit the official JONSBO AIO app before connecting to DS339.");
   string directory=Path.Combine(AppPaths.Root,"Native","DS339");if(!File.Exists(Path.Combine(directory,"MSDISPLAYSDKWRRAPER.dll")))throw new FileNotFoundException("Studio DS339 runtime is missing. Extract the complete standalone ZIP.");SetDllDirectory(directory);
   onAttach=(h,r,n)=>{for(int i=0;i<n;i++){int width=Marshal.ReadInt32(r,i*12),height=Marshal.ReadInt32(r,i*12+4);if(width==376&&height==960){handle=h;attached.Set();break;}}};onDetach=h=>{if(h==handle){handle=0;attached.Reset();}};
   Register(Marshal.GetFunctionPointerForDelegate(onAttach),Marshal.GetFunctionPointerForDelegate(onDetach));var data=new Edid{mode=2,replace="",count=1,timings=new byte[192]};uint[] timing={171,7,488,996,376,960,2916,6000,312,66,112,10};for(int i=0;i<timing.Length;i++)Array.Copy(BitConverter.GetBytes(timing[i]),0,data.timings,i*4,4);IntPtr ptr=Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Edid)));
   try{Marshal.StructureToPtr(data,ptr,false);int result=Start(1,ptr);if(result!=0)throw new IOException("DS339 display driver failed to start ("+result+").");started=true;Enable(true);if(!attached.Wait(5000)||handle==0)throw new IOException("DS339 USB display did not attach.");IntPtr sn=Marshal.AllocHGlobal(256),resolution=Marshal.AllocHGlobal(12);try{for(int i=0;i<256;i++)Marshal.WriteByte(sn,i,0);if(ReadSN(handle,sn)>0)serialNumber=Marshal.PtrToStringAnsi(sn);Marshal.StructureToPtr(new Resolution{width=376,height=960,refresh=60},resolution,false);if(SetVideoParam(handle,resolution)!=0)throw new IOException("DS339 rejected its 376 × 960 mode.");}finally{Marshal.FreeHGlobal(sn);Marshal.FreeHGlobal(resolution);}if(!serialNumber.StartsWith("VMAX",StringComparison.OrdinalIgnoreCase))throw new IOException("DS339 did not return a valid serial number.");decoded=new Bitmap(376,960,PixelFormat.Format24bppRgb);frame=new Bitmap(376,960,PixelFormat.Format32bppArgb);graphics=Graphics.FromImage(frame);codec=new TurboCodec();}catch{Dispose();throw;}finally{Marshal.FreeHGlobal(ptr);}
  }
  public void Send(byte[] jpeg){if(handle==0)throw new IOException("DS339 disconnected.");codec.Decode(jpeg,decoded);graphics.DrawImageUnscaled(decoded,0,0);var pixels=frame.LockBits(new Rectangle(0,0,376,960),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);IntPtr picture=Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Picture)));try{Marshal.StructureToPtr(new Picture{width=376,height=960,data=pixels.Scan0},picture,false);int result=SendPicture(handle,picture,false);if(result!=0)throw new IOException("DS339 frame transfer failed ("+result+").");}finally{Marshal.FreeHGlobal(picture);frame.UnlockBits(pixels);}}
  public void Dispose(){if(disposed)return;disposed=true;if(graphics!=null)graphics.Dispose();if(frame!=null)frame.Dispose();if(decoded!=null)decoded.Dispose();if(codec!=null)codec.Dispose();if(started){try{Stop();}catch{}}attached.Dispose();}
 }
}
