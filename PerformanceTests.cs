using System;
using System.Diagnostics;
using System.Threading;
using Newtonsoft.Json.Linq;
namespace JonsboStudio {
 static class PerformanceTests {
  public static void PreviewTiming(){using(var bitmap=new System.Drawing.Bitmap(376,960,System.Drawing.Imaging.PixelFormat.Format32bppArgb))using(var encoder=new PreviewEncoder()){using(var graphics=System.Drawing.Graphics.FromImage(bitmap))graphics.Clear(System.Drawing.Color.CornflowerBlue);var process=Process.GetCurrentProcess();var before=process.TotalProcessorTime;var watch=Stopwatch.StartNew();for(int i=0;i<600;i++)encoder.EncodePixels(bitmap,270,960,376);Console.WriteLine("Preview CPU milliseconds/frame: "+(process.TotalProcessorTime-before).TotalMilliseconds/600+"; wall "+watch.Elapsed.TotalMilliseconds/600);}}
  public static void SelfTest(){
   using(var stop=new ManualResetEvent(false))using(var pacer=new FramePacer(stop)){if(pacer.Wait(1))throw new Exception("Timer reported cancellation before stop.");stop.Set();if(!pacer.Wait(1000))throw new Exception("Frame timer ignored stop.");}

   var theme=new ThemeDocument{Json=ThemeDocument.Blank("Raw frame test",96,40),Folder=AppPaths.Root,Background=""};
   theme.Json["DisplayTexts"]=new JArray(new JObject{{"TextType","CpuUsage"},{"FontSize",16},{"FontName","Segoe UI"},{"Color","#FFFFFFFF"},{"Left",4},{"Top",4}});
   var snapshot=new SensorSnapshot{version=1};snapshot.values["CpuUsage"]="12";
   using(var codec=new TurboCodec())foreach(int rotation in new[]{0,90,180,270}){
    snapshot.version++;snapshot.values["CpuUsage"]="12";
    int w=rotation==90||rotation==270?40:96,h=rotation==90||rotation==270?96:40;
    using(var input=new System.Drawing.Bitmap(96,40,System.Drawing.Imaging.PixelFormat.Format24bppRgb)){
     using(var graphics=System.Drawing.Graphics.FromImage(input)){graphics.Clear(System.Drawing.Color.FromArgb(24,80,140));using(var brush=new System.Drawing.SolidBrush(System.Drawing.Color.LimeGreen))graphics.FillRectangle(brush,48,0,48,20);using(var brush=new System.Drawing.SolidBrush(System.Drawing.Color.Gold))graphics.FillRectangle(brush,48,20,48,20);}if(rotation==90)input.RotateFlip(System.Drawing.RotateFlipType.Rotate90FlipNone);else if(rotation==180)input.RotateFlip(System.Drawing.RotateFlipType.Rotate180FlipNone);else if(rotation==270)input.RotateFlip(System.Drawing.RotateFlipType.Rotate270FlipNone);
     byte[] jpeg=codec.Encode(input,90);
     using(var bgr=new System.Drawing.Bitmap(w,h,System.Drawing.Imaging.PixelFormat.Format24bppRgb))using(var bgra=new System.Drawing.Bitmap(w,h,System.Drawing.Imaging.PixelFormat.Format32bppArgb)){
      codec.Decode(jpeg,bgr);codec.Decode(jpeg,bgra);if(bgr.GetPixel(w-1,h-1).ToArgb()!=bgra.GetPixel(w-1,h-1).ToArgb())throw new Exception("BGRA decoding changed color or alpha.");
     }
     using(var renderer=new ThemeRenderer(theme,rotation,true,true,true,int.MinValue,int.MaxValue,true))using(var preview=new PreviewEncoder()){
      var pixels=renderer.Render(jpeg,snapshot);if(pixels.Width!=w||pixels.Height!=h||pixels.PixelFormat!=System.Drawing.Imaging.PixelFormat.Format32bppArgb)throw new Exception("Raw frame geometry changed.");
      byte[] first=preview.EncodePixels(pixels,(360-rotation)%360,96,40);snapshot.version++;snapshot.values["CpuUsage"]="97";byte[] next=preview.EncodePixels(renderer.Render(jpeg,snapshot),(360-rotation)%360,96,40);
      if(Convert.ToBase64String(first)==Convert.ToBase64String(next))throw new Exception("Raw preview did not update live text.");
      using(var stream=new System.IO.MemoryStream(next))using(var image=new System.Drawing.Bitmap(stream)){if(image.Width!=320||image.Height!=133)throw new Exception("Raw preview rotation changed logical dimensions.");var top=image.GetPixel(280,25);var bottom=image.GetPixel(280,110);if(top.G<top.R+50||bottom.R<180||bottom.G<140||bottom.B>80)throw new Exception("Raw preview did not restore upright quadrant colors at rotation "+rotation);}
     }
    }
   }
   Console.WriteLine("PASS: BGR/BGRA color parity, all native rotations, reduced preview and live sensor overlay updates.");
  }
  public static void Run(string first,string second,bool hardware){
   var ds916=StudioSettings.ForDevice("COM12");var ds339=StudioSettings.ForDevice(MsDisplayTransport.PortId);
   using(var a=new PlaybackEngine(ThemeDocument.Load(first),ds916,hardware))using(var b=new PlaybackEngine(ThemeDocument.Load(second),ds339,hardware)){
    a.Start();b.Start();var warm=Stopwatch.StartNew();while((!a.PreparedComplete||!b.PreparedComplete||a.Latest==null||b.Latest==null)&&warm.ElapsedMilliseconds<120000&&a.Error.Length==0&&b.Error.Length==0)Thread.Sleep(100);
    if(!a.PreparedComplete||!b.PreparedComplete||a.Latest==null||b.Latest==null)throw new Exception("Playback did not become ready: "+a.Error+" "+b.Error);
    Thread.Sleep(2000);var process=Process.GetCurrentProcess();TimeSpan cpu=process.TotalProcessorTime;long sentA=a.Sent,sentB=b.Sent,freshA=a.Changed,freshB=b.Changed;var watch=Stopwatch.StartNew();Thread.Sleep(15000);process.Refresh();double seconds=watch.Elapsed.TotalSeconds;
    var result=new JObject{{"cpuPercent",100*(process.TotalProcessorTime-cpu).TotalSeconds/seconds/Environment.ProcessorCount},{"efficiencyCores",CpuScheduling.Enabled},{"hardware",hardware},{"ramMB",process.WorkingSet64/1048576.0},{"ds916SentFps",(a.Sent-sentA)/seconds},{"ds339SentFps",(b.Sent-sentB)/seconds},{"ds916FreshFps",(a.Changed-freshA)/seconds},{"ds339FreshFps",(b.Changed-freshB)/seconds},{"ds916RenderMs",a.AverageEncodeMs},{"ds339RenderMs",b.AverageEncodeMs},{"errors",a.Error+" "+b.Error}};
    Console.WriteLine(result);if(a.Error.Length>0||b.Error.Length>0||a.Produced==0||b.Produced==0)throw new Exception("Playback failed.");if(hardware&&((a.Sent-sentA)/seconds<55||(b.Sent-sentB)/seconds<55))throw new Exception("USB frame rate fell below 55 FPS.");
   }
  }
 }
}
