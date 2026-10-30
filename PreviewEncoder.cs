using System;using System.IO;using System.Drawing;using System.Drawing.Imaging;using System.Drawing.Drawing2D;
namespace JonsboStudio {
 sealed class PreviewEncoder:IDisposable {
  Bitmap output;Graphics graphics;readonly TurboCodec codec=new TurboCodec();public int Width,Height;
  public byte[] Encode(byte[] jpeg,int inverseRotation,int logicalWidth,int logicalHeight){double scale=Math.Min(400.0/logicalWidth,225.0/logicalHeight);int w=Math.Max(2,(int)(logicalWidth*scale)),h=Math.Max(2,(int)(logicalHeight*scale));if(output==null||w!=Width||h!=Height){if(graphics!=null)graphics.Dispose();if(output!=null)output.Dispose();Width=w;Height=h;output=new Bitmap(w,h,PixelFormat.Format24bppRgb);graphics=Graphics.FromImage(output);graphics.InterpolationMode=InterpolationMode.Low;graphics.CompositingQuality=CompositingQuality.HighSpeed;}
   using(var stream=new MemoryStream(jpeg,false))using(var image=Image.FromStream(stream,false,false)){graphics.ResetTransform();if(inverseRotation==90){graphics.TranslateTransform(w,0);graphics.RotateTransform(90);graphics.DrawImage(image,0,0,h,w);}else if(inverseRotation==270){graphics.TranslateTransform(0,h);graphics.RotateTransform(270);graphics.DrawImage(image,0,0,h,w);}else if(inverseRotation==180){graphics.TranslateTransform(w,h);graphics.RotateTransform(180);graphics.DrawImage(image,0,0,w,h);}else graphics.DrawImage(image,0,0,w,h);return codec.Encode(output,52);}
  }
  public void Dispose(){if(graphics!=null)graphics.Dispose();if(output!=null)output.Dispose();codec.Dispose();}
 }
}
