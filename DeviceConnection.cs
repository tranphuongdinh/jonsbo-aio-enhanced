using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
namespace JonsboStudio {
 // All USB operations, including a manual reconnect, run on the display's sender.
 // A failed device never disposes or restarts the other display's session/cache.
 sealed class DeviceConnection:IDisposable {
  public static bool Trace;
  readonly Func<IDisplayTransport> factory;readonly Func<string,string> resolve;readonly Action<Exception> log;readonly Stopwatch clock=Stopwatch.StartNew();
  IDisplayTransport transport;int requested,failures,generation;double retryAt;string serial="";int width,height;
  public volatile string State="disconnected",Port;volatile string message="Not connected";long operationAt;public string Message{get{return State!="connected"&&clock.ElapsedMilliseconds-Interlocked.Read(ref operationAt)>12000?message+" Windows is not responding; reset this USB connection or restart Windows.":message;}private set{message=value;}}
  public int Generation{get{return Volatile.Read(ref generation);}}public bool Connected{get{return State=="connected";}}public IDisplayTransport Transport{get{return transport;}}
  public int NativeWidth{get{return width;}}public int NativeHeight{get{return height;}}public string SerialNumber{get{return serial;}}
  public DeviceConnection(string port,Func<IDisplayTransport> create=null,Func<string,string> resolver=null,Action<Exception> logger=null){Port=port;factory=create??(()=>port==MsDisplayTransport.PortId?(IDisplayTransport)new MsDisplayTransport():new JonsboTransport());string identity="";if(create==null&&port!=MsDisplayTransport.PortId)try{var device=JonsboTransport.Discover().FirstOrDefault(d=>d.port==port);identity=device==null?"":device.hardwareId;}catch{}resolve=resolver??(current=>ResolvePort(current,identity));log=logger??(error=>FirstRunSetup.Log(error,"USB connection "+Port));}
  internal static string ResolvePort(string current,string identity){return ResolvePort(current,identity,JonsboTransport.Discover());}
  internal static string ResolvePort(string current,string identity,IEnumerable<DeviceInfo> devices){if(current==MsDisplayTransport.PortId)return current;var original=devices.FirstOrDefault(d=>!string.IsNullOrEmpty(identity)&&string.Equals(d.hardwareId,identity,StringComparison.OrdinalIgnoreCase));if(original!=null)return original.port;var same=devices.FirstOrDefault(d=>d.port==current);if(same!=null&&(string.IsNullOrEmpty(identity)||string.Equals(same.hardwareId,identity,StringComparison.OrdinalIgnoreCase)))return same.port;var candidates=devices.Where(d=>d.name=="JONSBO DS916").ToArray();if(candidates.Length==1)return candidates[0].port;throw new IOException("DS916 is not detected. Check its USB connection and choose Refresh devices.");}
  public void Connect(){Interlocked.Exchange(ref operationAt,clock.ElapsedMilliseconds);State="connecting";Message="Connecting…";var next=factory();try{if(Trace)Console.WriteLine("USB: resolving "+Port);string port=resolve(Port);if(Trace)Console.WriteLine("USB: connecting "+port);next.Connect(port);if(width!=0&&(next.NativeWidth!=width||next.NativeHeight!=height||next.SerialNumber!=serial))throw new IOException("A different display appeared on this interface. Refresh devices before applying its theme.");width=next.NativeWidth;height=next.NativeHeight;serial=next.SerialNumber;Port=port;transport=next;failures=0;Interlocked.Increment(ref generation);State="connected";Message="Connected · "+port;}catch{try{next.Dispose();}catch{}throw;}}
  public void RequestReconnect(){Interlocked.Exchange(ref requested,1);}
  public bool EnsureConnected(WaitHandle stop){if(stop.WaitOne(0))return false;if(Interlocked.Exchange(ref requested,0)!=0){State="reconnecting";Message="Releasing the previous USB connection…";Interlocked.Exchange(ref operationAt,clock.ElapsedMilliseconds);Close();retryAt=0;failures=0;State="reconnecting";Message="Reconnecting…";}if(transport!=null)return true;double remaining=retryAt-clock.Elapsed.TotalMilliseconds;if(remaining>0){stop.WaitOne(Math.Min(250,Math.Max(1,(int)Math.Ceiling(remaining))));return false;}try{Connect();return true;}catch(Exception error){if(!Recoverable(error))throw;Failed(error);return false;}}
  public bool Send(Action<IDisplayTransport> send){try{send(transport);return true;}catch(Exception error){if(!Recoverable(error))throw;Failed(error);return false;}}
  internal static bool Recoverable(Exception error){return error is IOException||error is TimeoutException||error is UnauthorizedAccessException||error is InvalidOperationException;}
  internal static int RetryDelay(int failure){return 1000*(1<<Math.Min(3,Math.Max(0,failure-1)));}
  void Failed(Exception error){State="reconnecting";Message=error.Message+" Releasing the previous USB connection…";Interlocked.Exchange(ref operationAt,clock.ElapsedMilliseconds);Close();failures++;int delay=RetryDelay(failures);retryAt=clock.Elapsed.TotalMilliseconds+delay;State="reconnecting";Message=error.Message+" Retrying in "+delay/1000+"s.";if(failures==1||(failures&(failures-1))==0)try{log(error);}catch{}}
  void Close(){var previous=transport;transport=null;if(previous!=null)try{previous.Dispose();}catch(Exception error){try{log(error);}catch{}}}
  public void Dispose(){Close();State="disconnected";Message="Stopped";}
 }
}
