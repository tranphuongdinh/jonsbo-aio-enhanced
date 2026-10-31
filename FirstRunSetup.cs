using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Management;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;

namespace JonsboStudio {
 static class FirstRunSetup {
  internal static bool MissingRuntime(Exception error){return error is WebView2RuntimeNotFoundException||error is FileNotFoundException||error.HResult==unchecked((int)0x80070002)||error.HResult==unchecked((int)0x80070003);}
  static bool NeedsWebView(){try{return string.IsNullOrEmpty(CoreWebView2Environment.GetAvailableBrowserVersionString());}catch(Exception error){if(MissingRuntime(error))return true;throw;}}
  // A version registry entry can outlive the browser files. Test creation too and repair once.
  internal static async Task<T> WithRuntimeRepair<T>(Func<Task<T>> create,Action repair){try{return await create();}catch(Exception error){if(!MissingRuntime(error))throw;}repair();try{return await create();}catch(Exception error){throw new InvalidOperationException("The Microsoft WebView2 interface could not start after setup. Restart Windows and reopen Studio. If it still fails, install or repair Microsoft WebView2 Runtime using the installer in Native\\WebView2. Extract the complete ZIP before running Studio.",error);}}
  public static Task<bool> InitializeWebViewAsync(Microsoft.Web.WebView2.WinForms.WebView2 web){return WithRuntimeRepair(async()=>{var environment=await CoreWebView2Environment.CreateAsync(null,AppPaths.Data("WebView2"));await web.EnsureCoreWebView2Async(environment);return true;},()=>Ensure(true));}
  static bool NeedsDriver(){using(var query=new ManagementObjectSearcher("SELECT PNPDeviceID,PNPClass,ConfigManagerErrorCode FROM Win32_PnPEntity"))foreach(ManagementObject item in query.Get()){string id=item["PNPDeviceID"] as string;if(id!=null&&id.StartsWith(@"USB\VID_345F&PID_9132&MI_03",StringComparison.OrdinalIgnoreCase)&&(Convert.ToInt32(item["ConfigManagerErrorCode"])!=0||!string.Equals(item["PNPClass"] as string,"MSDisplay",StringComparison.OrdinalIgnoreCase)))return true;}return false;}
  public static void Ensure(bool repairWebView=false){ValidatePackage();bool web=repairWebView||NeedsWebView(),driver=NeedsDriver();if(!web&&!driver)return;
   string script=Path.Combine(AppPaths.Root,"Assets","Setup","Prerequisites.ps1");if(!File.Exists(script))throw new FileNotFoundException("First-run setup is missing. Extract the complete Studio ZIP.",script);
   Exception failure=null;using(var dialog=new Form{Text="JONSBO Studio setup",ClientSize=new Size(440,125),StartPosition=FormStartPosition.CenterScreen,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false,ControlBox=false,BackColor=Color.FromArgb(24,35,43),ForeColor=Color.White}){
    dialog.Controls.Add(new Label{Text="Preparing JONSBO Studio…\r\n"+(web?"Installing Microsoft WebView2 from the bundled installer.":"Installing the DS339 USB driver. Approve the Windows prompt."),AutoSize=false,Bounds=new Rectangle(20,18,400,58)});dialog.Controls.Add(new ProgressBar{Style=ProgressBarStyle.Marquee,Bounds=new Rectangle(20,90,400,12)});
    dialog.Shown+=async(s,e)=>{try{await Task.Run(()=>{string shell=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),@"WindowsPowerShell\v1.0\powershell.exe");var info=new ProcessStartInfo(shell,"-NoProfile -ExecutionPolicy Bypass -File \""+script+"\""+(web?" -InstallWebView":"")){UseShellExecute=false,CreateNoWindow=true};using(var process=Process.Start(info)){process.WaitForExit();if(process.ExitCode!=0)throw new InvalidOperationException("Prerequisite setup did not complete. Check your internet connection and approve the USB driver prompt, then reopen Studio.");}});}catch(Exception ex){failure=ex;}dialog.Close();};dialog.ShowDialog();
   }if(failure!=null)throw failure;if(NeedsWebView())throw new InvalidOperationException("Microsoft WebView2 installation is incomplete. Restart Windows, then reopen Studio.");
  }
  public static void ValidatePackage(){foreach(string name in new[]{"WebView2Loader.dll","Microsoft.Web.WebView2.Core.dll","Microsoft.Web.WebView2.WinForms.dll","turbojpeg.dll","Assets/Web/index.html","Assets/Web/app.js","Assets/Web/style.css","Assets/Setup/Prerequisites.ps1","Native/FFmpeg/ffmpeg.exe","Native/DS339/MSDISPLAYSDKWRRAPER.dll","Native/DS339/libusb0.dll"})if(!File.Exists(Path.Combine(AppPaths.Root,name.Replace('/',Path.DirectorySeparatorChar))))throw new FileNotFoundException("The Studio package is incomplete: "+name+" is missing. Extract ALL files from JonsboStudio-v1.0.0-win-x64.zip into one folder, then open JonsboStudio.exe there. Do not run the executable inside the ZIP.");}
  public static string Log(Exception error,string stage){string path=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"JonsboStudio","Logs","startup.log");try{Directory.CreateDirectory(Path.GetDirectoryName(path));File.AppendAllText(path,DateTime.Now.ToString("s")+" "+stage+"\r\n"+error+"\r\n");}catch{}return path;}
 }
}
