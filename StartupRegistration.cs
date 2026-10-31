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

  internal static bool MissingTask(Exception error){return error.HResult==unchecked((int)0x80070002)||error.HResult==unchecked((int)0x80070003);}
  internal static bool TaskEnabled(string name){dynamic task=Connect().GetFolder("\\").GetTask(name);return (bool)task.Enabled;}
  // .NET maps Task Scheduler's missing-task HRESULT to FileNotFoundException,
  // not necessarily COMException. Optional startup status must never block the UI.
  internal static bool ReadEnabled(Func<bool> readTask,Func<bool> readLegacy){try{return readTask();}catch(Exception error){if(!MissingTask(error))FirstRunSetup.Log(error,"Windows startup status (non-fatal)");try{return readLegacy();}catch(Exception legacyError){FirstRunSetup.Log(legacyError,"Windows startup legacy status (non-fatal)");return false;}}}
  public static bool IsEnabled(){return ReadEnabled(()=>TaskEnabled(TaskName),LegacyEnabled);}
  internal static void DeleteIfPresent(Action delete){try{delete();}catch(Exception error){if(!MissingTask(error))throw;}}
  internal static bool FreshInstallStatus(){return ReadEnabled(()=>TaskEnabled("JonsboStudio-absent-test-"+Guid.NewGuid().ToString("N")),()=>false);}
  internal static void SelfTest(){
   string name="JonsboStudio-absent-test-"+Guid.NewGuid().ToString("N");bool missing=false;
   try{TaskEnabled(name);}catch(Exception error){if(!MissingTask(error))throw;missing=true;Console.WriteLine("Reproduced missing task: "+error.GetType().Name+" / 0x"+error.HResult.ToString("X8"));}
   if(!missing||ReadEnabled(()=>TaskEnabled(name),()=>false))throw new Exception("A fresh install must report startup disabled.");
   DeleteIfPresent(()=>{dynamic folder=Connect().GetFolder("\\");folder.DeleteTask(name,0);});
   if(!ReadEnabled(()=>{throw new FileNotFoundException();},()=>true))throw new Exception("Legacy startup fallback was lost.");
   if(ReadEnabled(()=>false,()=>true))throw new Exception("Disabled scheduled task must stay disabled.");
   if(ReadEnabled(()=>{throw new UnauthorizedAccessException("Simulated scheduler access denied");},()=>false))throw new Exception("Optional scheduler failure must not enable startup.");
   DeleteIfPresent(()=>{throw new FileNotFoundException();});DeleteIfPresent(()=>{throw new COMException("Missing",unchecked((int)0x80070002));});
   bool denied=false;try{DeleteIfPresent(()=>{throw new UnauthorizedAccessException();});}catch(UnauthorizedAccessException){denied=true;}if(!denied)throw new Exception("Startup changes must report access errors.");
   Console.WriteLine("PASS: absent task, legacy fallback, disabled task, unavailable scheduler and idempotent startup disable.");
  }

  public static void Set(bool enabled){dynamic folder=Connect().GetFolder("\\");if(!enabled){DeleteIfPresent(()=>{folder.DeleteTask(TaskName,0);});RemoveLegacy();return;}
   string sid=WindowsIdentity.GetCurrent().User.Value;string executable=Path.Combine(AppPaths.Root,"JonsboStudio.exe");if(!File.Exists(executable))executable=Application.ExecutablePath;
   dynamic service=Connect();dynamic definition=service.NewTask(0);definition.RegistrationInfo.Description="Start JONSBO Studio immediately at sign-in, hidden in the tray.";definition.Principal.UserId=sid;definition.Principal.LogonType=3;definition.Principal.RunLevel=0;
   dynamic trigger=definition.Triggers.Create(9);trigger.UserId=sid;trigger.Enabled=true;
   dynamic action=definition.Actions.Create(0);action.Path=executable;action.Arguments="--background";action.WorkingDirectory=AppPaths.Root;
   definition.Settings.Enabled=true;definition.Settings.Priority=2;definition.Settings.MultipleInstances=2;definition.Settings.DisallowStartIfOnBatteries=false;definition.Settings.StopIfGoingOnBatteries=false;definition.Settings.RunOnlyIfIdle=false;definition.Settings.RunOnlyIfNetworkAvailable=false;definition.Settings.ExecutionTimeLimit="PT0S";
   folder.RegisterTaskDefinition(TaskName,definition,6,sid,null,3,null);RemoveLegacy();
  }
 }
}
