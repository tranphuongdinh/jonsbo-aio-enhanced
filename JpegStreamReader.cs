using System;using System.IO;
namespace JonsboStudio {
 sealed class JpegStreamReader {
  readonly Stream stream;public JpegStreamReader(Stream value){stream=new BufferedStream(value,65536);}
  int Byte(){int b=stream.ReadByte();if(b<0)throw new EndOfStreamException("Truncated JPEG stream");return b;}
  void Put(MemoryStream m,int b){if(m.Length>=16*1024*1024)throw new InvalidDataException("JPEG exceeds 16 MB");m.WriteByte((byte)b);}
  public byte[] ReadFrame(){int first=stream.ReadByte();if(first<0)return null;if(first!=255||Byte()!=216)throw new InvalidDataException("Expected JPEG SOI");using(var m=new MemoryStream()){Put(m,255);Put(m,216);bool entropy=false;while(true){int marker;if(entropy){int b=Byte();Put(m,b);if(b!=255)continue;do{marker=Byte();Put(m,marker);}while(marker==255);if(marker==0||(marker>=208&&marker<=215))continue;}else{if(Byte()!=255)throw new InvalidDataException("Expected JPEG marker");Put(m,255);do{marker=Byte();Put(m,marker);}while(marker==255);}if(marker==217)return m.ToArray();if(marker==216||marker==0)throw new InvalidDataException("Invalid JPEG marker");int hi=Byte(),lo=Byte();Put(m,hi);Put(m,lo);int length=(hi<<8)|lo;if(length<2)throw new InvalidDataException("Invalid JPEG length");for(int i=2;i<length;i++)Put(m,Byte());entropy=marker==218;}}}
 }
}
