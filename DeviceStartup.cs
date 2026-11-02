using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
namespace JonsboStudio {
 // A blocked Windows driver must not keep the UI busy or open a second handle
 // on the same device. Cancellation waits for the outstanding call to unwind.
 sealed class DeviceStartup {
  readonly object gate=new object();readonly Dictionary<string,Task> pending=new Dictionary<string,Task>(StringComparer.OrdinalIgnoreCase);
  public bool IsPending(string port){lock(gate){Task task;return pending.TryGetValue(port,out task)&&!task.IsCompleted;}}
  // Keep startup foreground until Windows returns; CLR shutdown must not finalize
  // a half-constructed .NET SerialStream while its Open call is still in progress.
  public async Task Start(string port,Action start,Action cancel,Action cleanup,int timeout=12000){Task work;lock(gate){Task old;if(pending.TryGetValue(port,out old)&&!old.IsCompleted)throw new IOException("Windows is still finishing the previous USB operation for "+port+". Unplug and reconnect that display before retrying.");var done=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);work=done.Task;pending[port]=work;var worker=new Thread(()=>{try{start();done.TrySetResult(true);}catch(Exception error){done.TrySetException(error);}}){IsBackground=false,Name="JONSBO USB open "+port};worker.Start();}
   if(await Task.WhenAny(work,Task.Delay(timeout))!=work){cancel();var finish=work.ContinueWith(task=>{var observed=task.Exception;cleanup();},TaskScheduler.Default);lock(gate)pending[port]=finish;throw new TimeoutException("Windows did not open "+port+" within "+timeout/1000+" seconds. Unplug its USB cable for 10 seconds, reconnect, then retry. Other displays can keep playing.");}
   try{await work;}finally{lock(gate){Task current;if(pending.TryGetValue(port,out current)&&ReferenceEquals(current,work))pending.Remove(port);}}
  }
 }
}
