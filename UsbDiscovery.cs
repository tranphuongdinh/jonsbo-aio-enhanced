using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
namespace JonsboStudio {
 static class UsbDiscovery {
  [StructLayout(LayoutKind.Sequential)]struct DeviceData{public uint size;public Guid classGuid;public uint instance;public IntPtr reserved;}
  [DllImport("setupapi.dll",CharSet=CharSet.Unicode,SetLastError=true)]static extern IntPtr SetupDiGetClassDevs(IntPtr classGuid,string enumerator,IntPtr parent,uint flags);
  [DllImport("setupapi.dll",SetLastError=true)]static extern bool SetupDiEnumDeviceInfo(IntPtr set,uint index,ref DeviceData data);
  [DllImport("setupapi.dll",CharSet=CharSet.Unicode,SetLastError=true)]static extern bool SetupDiGetDeviceInstanceId(IntPtr set,ref DeviceData data,StringBuilder id,uint length,out uint required);
  [DllImport("setupapi.dll")]static extern bool SetupDiDestroyDeviceInfoList(IntPtr set);
  [DllImport("cfgmgr32.dll")]static extern uint CM_Get_DevNode_Status(out uint status,out uint problem,uint instance,uint flags);
  public static List<DeviceInfo> Displays(IList<DeviceInfo> serial=null){var devices=new List<DeviceInfo>();IntPtr set=SetupDiGetClassDevs(IntPtr.Zero,"USB",IntPtr.Zero,6);if(set==new IntPtr(-1))throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());try{for(uint index=0;;index++){var data=new DeviceData{size=(uint)Marshal.SizeOf(typeof(DeviceData))};if(!SetupDiEnumDeviceInfo(set,index,ref data)){int code=Marshal.GetLastWin32Error();if(code!=259)throw new System.ComponentModel.Win32Exception(code);break;}var id=new StringBuilder(1024);uint required;if(!SetupDiGetDeviceInstanceId(set,ref data,id,(uint)id.Capacity,out required))continue;string value=id.ToString();uint status,problem;bool known=CM_Get_DevNode_Status(out status,out problem,data.instance,0)==0;bool ready=known&&problem==0&&(status&8)!=0;if(serial!=null)foreach(var item in serial)if(string.Equals("USB\\"+item.hardwareId,value,StringComparison.OrdinalIgnoreCase)){item.ready=ready;item.problemCode=known?(int)problem:-1;}if(value.StartsWith(@"USB\VID_345F&PID_9132&MI_03\",StringComparison.OrdinalIgnoreCase))devices.Add(new DeviceInfo{port=MsDisplayTransport.PortId,name="JONSBO DS339",hardwareId=value,width=376,height=960,ready=ready,problemCode=known?(int)problem:-1});}}finally{SetupDiDestroyDeviceInfoList(set);}return devices;}
 }
}
