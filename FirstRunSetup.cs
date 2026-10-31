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
  static bool NeedsWebView(){try{return string.IsNullOrEmpty(CoreWebView2Environment.GetAvailableBrowserVersionString());}catch(WebView2RuntimeNotFoundException){return true;}}
  static bool NeedsDriver(){using(var query=new ManagementObjectSearcher("SELECT PNPDeviceID,PNPClass,ConfigManagerErrorCode FROM Win32_PnPEntity"))foreach(ManagementObject item in query.Get()){string id=item["PNPDeviceID"] as string;if(id!=null&&id.StartsWith(@"USB\VID_345F&PID_9132&MI_03",StringComparison.OrdinalIgnoreCase)&&(Convert.ToInt32(item["ConfigManagerErrorCode"])!=0||!string.Equals(item["PNPClass"] as string,"MSDisplay",StringComparison.OrdinalIgnoreCase)))return true;}return false;}
  public static void Ensure(){bool web=NeedsWebView(),driver=NeedsDriver();if(!web&&!driver)return;
   string script=Path.Combine(AppPaths.Root,"Assets","Setup","Prerequisites.ps1");if(!File.Exists(script))throw new FileNotFoundException("First-run setup is missing. Extract the complete Studio ZIP.",script);
   Exception failure=null;using(var dialog=new Form{Text="JONSBO Studio setup",ClientSize=new Size(440,125),StartPosition=FormStartPosition.CenterScreen,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false,ControlBox=false,BackColor=Color.FromArgb(24,35,43),ForeColor=Color.White}){
    dialog.Controls.Add(new Label{Text="Preparing JONSBO Studio…\r\n"+(web?"Installing Microsoft WebView2. Internet access is required.":"Installing the DS339 USB driver. Approve the Windows prompt."),AutoSize=false,Bounds=new Rectangle(20,18,400,58)});dialog.Controls.Add(new ProgressBar{Style=ProgressBarStyle.Marquee,Bounds=new Rectangle(20,90,400,12)});
    dialog.Shown+=async(s,e)=>{try{await Task.Run(()=>{string shell=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),@"WindowsPowerShell\v1.0\powershell.exe");var info=new ProcessStartInfo(shell,"-NoProfile -ExecutionPolicy Bypass -File \""+script+"\""+(web?" -InstallWebView":"")){UseShellExecute=false,CreateNoWindow=true};using(var process=Process.Start(info)){process.WaitForExit();if(process.ExitCode!=0)throw new InvalidOperationException("Prerequisite setup did not complete. Check your internet connection and approve the USB driver prompt, then reopen Studio.");}});}catch(Exception ex){failure=ex;}dialog.Close();};dialog.ShowDialog();
   }if(failure!=null)throw failure;if(NeedsWebView())throw new InvalidOperationException("Microsoft WebView2 installation is incomplete. Restart Windows, then reopen Studio.");
  }
 }
}
