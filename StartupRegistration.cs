using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Windows.Forms;
using Microsoft.Win32;

namespace JonsboStudio {
 static class StartupRegistration {
  const string TaskName="JONSBO Studio";
  const string RunKey=@"Software\Microsoft\Windows\CurrentVersion\Run";

  static dynamic Connect(){var type=Type.GetTypeFromProgID("Schedule.Service");if(type==null)throw new InvalidOperationException("Windows Task Scheduler is unavailable.");dynamic service=Activator.CreateInstance(type);service.Connect();return service;}

  static bool LegacyEnabled(){using(var key=Registry.CurrentUser.OpenSubKey(RunKey))return key!=null&&key.GetValue("JonsboStudio")!=null;}
  static void RemoveLegacy(){using(var key=Registry.CurrentUser.OpenSubKey(RunKey,true))if(key!=null)key.DeleteValue("JonsboStudio",false);}

  public static bool IsEnabled(){try{dynamic task=Connect().GetFolder("\\").GetTask(TaskName);return (bool)task.Enabled;}catch(COMException ex){if(unchecked((uint)ex.ErrorCode)!=0x80070002)throw;return LegacyEnabled();}}

  public static void Set(bool enabled){dynamic folder=Connect().GetFolder("\\");if(!enabled){try{folder.DeleteTask(TaskName,0);}catch(COMException ex){if(unchecked((uint)ex.ErrorCode)!=0x80070002)throw;}RemoveLegacy();return;}
   string sid=WindowsIdentity.GetCurrent().User.Value;string executable=Path.Combine(AppPaths.Root,"JonsboStudio.exe");if(!File.Exists(executable))executable=Application.ExecutablePath;
   dynamic service=Connect();dynamic definition=service.NewTask(0);definition.RegistrationInfo.Description="Start JONSBO Studio immediately at sign-in, hidden in the tray.";definition.Principal.UserId=sid;definition.Principal.LogonType=3;definition.Principal.RunLevel=0;
   dynamic trigger=definition.Triggers.Create(9);trigger.UserId=sid;trigger.Enabled=true;
   dynamic action=definition.Actions.Create(0);action.Path=executable;action.Arguments="--background";action.WorkingDirectory=AppPaths.Root;
   definition.Settings.Enabled=true;definition.Settings.Priority=2;definition.Settings.MultipleInstances=2;definition.Settings.DisallowStartIfOnBatteries=false;definition.Settings.StopIfGoingOnBatteries=false;definition.Settings.RunOnlyIfIdle=false;definition.Settings.RunOnlyIfNetworkAvailable=false;definition.Settings.ExecutionTimeLimit="PT0S";
   folder.RegisterTaskDefinition(TaskName,definition,6,sid,null,3,null);RemoveLegacy();
  }
 }
}
