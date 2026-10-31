param([string]$OutputName='JonsboStudio.exe')
$ErrorActionPreference='Stop'
if([IO.Path]::GetFileName($OutputName)-ne $OutputName -or $OutputName -notmatch '\.exe$'){throw 'OutputName must be an exe filename.'}
Push-Location $PSScriptRoot
try{
 $compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
 $references=@('/r:System.Drawing.dll','/r:System.Windows.Forms.dll','/r:System.Core.dll','/r:System.Management.dll',('/r:'+ (Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\netstandard.dll')),'/r:Runtime\Newtonsoft.Json.dll','/r:Runtime\ICSharpCode.SharpZipLib.dll','/r:Runtime\Microsoft.Web.WebView2.Core.dll','/r:Runtime\Microsoft.Web.WebView2.WinForms.dll','/r:Runtime\LibreHardwareMonitorLib.dll')
 $sources=@('AssemblyInfo.cs','Program.cs','AppPaths.cs','ThemeDocument.cs','ThemeRepository.cs','OnlineCatalog.cs','JonsboTransport.cs','MsDisplayTransport.cs','TurboCodec.cs','Sensors.cs','ThemeRenderer.cs','PreparedStore.cs','JpegStreamReader.cs','PlaybackEngine.cs','PreviewEncoder.cs','AppForm.cs')
 & $compiler /nologo /platform:x64 /target:winexe /optimize+ '/win32icon:Assets\Icon\JonsboStudio.ico' "/out:$OutputName" $references $sources
 if($LASTEXITCODE -ne 0){throw 'Build failed.'}
 & $compiler /nologo /platform:x64 /target:exe /optimize+ '/win32icon:Assets\Icon\JonsboStudio.ico' /out:JonsboStudio.Diagnostics.exe $references $sources
 if($LASTEXITCODE -ne 0){throw 'Diagnostic build failed.'}
 foreach($file in Get-ChildItem Runtime -Filter '*.dll'){$target=Join-Path $PSScriptRoot $file.Name;if(-not(Test-Path $target)){Copy-Item -LiteralPath $file.FullName -Destination $target}elseif((Get-FileHash $file.FullName).Hash -ne (Get-FileHash $target).Hash){throw "Close the app before replacing runtime $($file.Name)."}}
 @'
<?xml version="1.0"?><configuration><startup><supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.8"/></startup><runtime><assemblyBinding xmlns="urn:schemas-microsoft-com:asm.v1"><dependentAssembly><assemblyIdentity name="Newtonsoft.Json" publicKeyToken="30ad4fe6b2a6aeed" culture="neutral"/><bindingRedirect oldVersion="0.0.0.0-13.0.0.0" newVersion="13.0.0.0"/></dependentAssembly></assemblyBinding></runtime></configuration>
'@ | Set-Content -Encoding UTF8 "$OutputName.config"
 Copy-Item -LiteralPath "$OutputName.config" -Destination 'JonsboStudio.Diagnostics.exe.config' -Force
 Write-Host "Built $OutputName and diagnostics"
}finally{Pop-Location}
