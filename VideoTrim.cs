using System;
using System.Globalization;
using System.IO;
using Newtonsoft.Json.Linq;
namespace JonsboStudio {
 sealed class VideoTrim {
  public double Start;public double? End;
  public static VideoTrim Read(JObject json){var trim=new VideoTrim{Start=Number(json["VideoTrimStart"],0)??0,End=Number(json["VideoTrimEnd"],null)};if(trim.Start<0||trim.End.HasValue&&trim.End.Value<=trim.Start)throw new InvalidDataException("Video trim end must be after start, and start must be zero or greater.");return trim;}
  static double? Number(JToken value,double? fallback){if(value==null||value.Type==JTokenType.Null)return fallback;if(value.Type!=JTokenType.Float&&value.Type!=JTokenType.Integer)throw new InvalidDataException("Video trim times must be numbers in seconds.");double number=(double)value;if(double.IsNaN(number)||double.IsInfinity(number))throw new InvalidDataException("Video trim times must be finite numbers.");return number;}
  public void Save(JObject json){json["VideoTrimStart"]=Start;json["VideoTrimEnd"]=End.HasValue?new JValue(End.Value):JValue.CreateNull();}
  public double Duration(double sourceDuration){if(sourceDuration<=0||double.IsNaN(sourceDuration)||double.IsInfinity(sourceDuration))throw new InvalidDataException("Unable to read video duration. Choose a video with a valid duration.");double end=Math.Min(End??sourceDuration,sourceDuration);if(Start>=end)throw new InvalidDataException("Video trim start is outside the video. Reset the trim or choose an earlier start.");return end-Start;}
  public string InputSeek{get{return Start>0?"-ss "+Start.ToString("0.#########",CultureInfo.InvariantCulture)+" ":"";}}
  public string OutputLimit(double duration){return "-t "+duration.ToString("0.#########",CultureInfo.InvariantCulture)+" ";}
 }
}
