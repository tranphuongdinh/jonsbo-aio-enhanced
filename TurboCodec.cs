using System;using System.Drawing;using System.Drawing.Imaging;using System.IO;using System.Runtime.InteropServices;
namespace JonsboStudio {
 sealed class TurboCodec:IDisposable {
  const int FastDct=2048,FastUpsample=256,NoReallocate=1024;
  IntPtr compressor,decompressor,buffer;ulong capacity;bool native;
  [DllImport("turbojpeg.dll",CallingConvention=CallingConvention.Cdecl)]static extern IntPtr tjInitCompress();
  [DllImport("turbojpeg.dll",CallingConvention=CallingConvention.Cdecl)]static extern IntPtr tjInitDecompress();
  [DllImport("turbojpeg.dll",CallingConvention=CallingConvention.Cdecl)]static extern ulong tjBufSize(int width,int height,int sampling);
  [DllImport("turbojpeg.dll",CallingConvention=CallingConvention.Cdecl)]static extern IntPtr tjAlloc(int bytes);
  [DllImport("turbojpeg.dll",CallingConvention=CallingConvention.Cdecl)]static extern void tjFree(IntPtr ptr);
  [DllImport("turbojpeg.dll",CallingConvention=CallingConvention.Cdecl)]static extern int tjDestroy(IntPtr handle);
  [DllImport("turbojpeg.dll",CallingConvention=CallingConvention.Cdecl)]static extern int tjCompress2(IntPtr handle,IntPtr source,int width,int pitch,int height,int pixelFormat,ref IntPtr destination,ref ulong size,int sampling,int quality,int flags);
  [DllImport("turbojpeg.dll",CallingConvention=CallingConvention.Cdecl)]static extern int tjDecompress2(IntPtr handle,byte[] source,ulong size,IntPtr destination,int width,int pitch,int height,int pixelFormat,int flags);
  public TurboCodec(){try{compressor=tjInitCompress();decompressor=tjInitDecompress();native=compressor!=IntPtr.Zero&&decompressor!=IntPtr.Zero;}catch{native=false;}}
  public byte[] Encode(Bitmap bitmap,int quality){
   if(native){bool bgra=bitmap.PixelFormat==PixelFormat.Format32bppArgb;ulong required=tjBufSize(bitmap.Width,bitmap.Height,2);if(required>capacity){if(buffer!=IntPtr.Zero)tjFree(buffer);capacity=required;buffer=tjAlloc((int)capacity);}var data=bitmap.LockBits(new Rectangle(0,0,bitmap.Width,bitmap.Height),ImageLockMode.ReadOnly,bgra?PixelFormat.Format32bppArgb:PixelFormat.Format24bppRgb);try{IntPtr target=buffer;ulong size=capacity;if(tjCompress2(compressor,data.Scan0,bitmap.Width,data.Stride,bitmap.Height,bgra?8:1,ref target,ref size,2,quality,NoReallocate|(quality<90?FastDct:0))!=0)throw new IOException("TurboJPEG compression failed.");var bytes=new byte[(int)size];Marshal.Copy(target,bytes,0,bytes.Length);return bytes;}finally{bitmap.UnlockBits(data);}}
   using(var stream=new MemoryStream()){ImageCodecInfo encoder=null;foreach(var c in ImageCodecInfo.GetImageEncoders())if(c.MimeType=="image/jpeg")encoder=c;using(var parameters=new EncoderParameters(1)){parameters.Param[0]=new EncoderParameter(System.Drawing.Imaging.Encoder.Quality,(long)quality);bitmap.Save(stream,encoder,parameters);}return stream.ToArray();}
  }
  public void Decode(byte[] jpeg,Bitmap output){bool bgra=output.PixelFormat==PixelFormat.Format32bppArgb;if(native){var data=output.LockBits(new Rectangle(0,0,output.Width,output.Height),ImageLockMode.WriteOnly,bgra?PixelFormat.Format32bppArgb:PixelFormat.Format24bppRgb);try{if(tjDecompress2(decompressor,jpeg,(ulong)jpeg.Length,data.Scan0,output.Width,data.Stride,output.Height,bgra?8:1,FastDct|FastUpsample)!=0)throw new IOException("TurboJPEG decompression failed.");}finally{output.UnlockBits(data);}return;}using(var stream=new MemoryStream(jpeg))using(var image=Image.FromStream(stream))using(var graphics=Graphics.FromImage(output))graphics.DrawImage(image,0,0,output.Width,output.Height);}
  public void Dispose(){if(buffer!=IntPtr.Zero)tjFree(buffer);if(compressor!=IntPtr.Zero)tjDestroy(compressor);if(decompressor!=IntPtr.Zero)tjDestroy(decompressor);buffer=compressor=decompressor=IntPtr.Zero;}
 }
}
