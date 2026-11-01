using System;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace JonsboStudio {
 // The store owns sign-in and purchases. Only its completed theme downloads enter Studio.
 sealed class CommunityThemeStore:Form {
  readonly WebView2 browser=new WebView2{Dock=DockStyle.Fill};
  readonly Label state=new Label{Dock=DockStyle.Bottom,Height=42,Padding=new Padding(12),ForeColor=Color.White};
  readonly string themeId;readonly Func<string,Task> imported;readonly byte[] testArchive;bool downloading,complete,trustedPage;
  public CommunityThemeStore(string id,string title,Func<string,Task> onImported,byte[] downloadTestArchive=null){
   if(!Regex.IsMatch(id??"",@"^community-[0-9]+$"))throw new ArgumentException("Invalid community theme.");
   themeId=id.Substring(10);imported=onImported;testArchive=downloadTestArchive;Text="JONSBO Studio · "+title;ClientSize=new Size(1050,780);StartPosition=FormStartPosition.CenterParent;BackColor=Color.FromArgb(24,35,43);
   var header=new Panel{Dock=DockStyle.Top,Height=78,Padding=new Padding(12)};
   header.Controls.Add(new Label{Dock=DockStyle.Fill,ForeColor=Color.White,Text="Sign in to the official store, then Download & apply.\r\nThe completed download applies to your selected display.\r\nPoints or purchase confirmation stays on the official website."});
   var apply=new Button{Text="Download && apply",Dock=DockStyle.Right,Width=160,ForeColor=Color.Black};
   apply.Click+=async(s,e)=>{try{if(browser.CoreWebView2==null||browser.Source==null||browser.Source.Host!="theme.screen-aio.com")return;string clicked=await browser.CoreWebView2.ExecuteScriptAsync("(()=>{const b=document.querySelector('.btn-download-theme[data-themeid=\""+themeId+"\"]');if(!b)return false;b.click();return true})()");if(clicked=="false")browser.CoreWebView2.Navigate("https://theme.screen-aio.com/theme/"+themeId+".html");}catch{state.Text="The store page is still loading. Try Download & apply again.";}};header.Controls.Add(apply);
   Controls.Add(browser);Controls.Add(header);Controls.Add(state);Shown+=async(s,e)=>await Initialize();
   FormClosing+=(s,e)=>{if(downloading){e.Cancel=true;state.Text="Wait for the download/import to finish, or cancel the download in the store.";}};
  }
  internal static bool AllowedDownload(string url){Uri uri;return Uri.TryCreate(url,UriKind.Absolute,out uri)&&uri.Scheme=="https"&&(uri.Host=="theme.screen-aio.com"||uri.Host=="res.screen-aio.com"||uri.Host=="res.hlinel.com");}
  internal static bool ThemeFile(string path){string extension=Path.GetExtension(path).ToLowerInvariant();return extension==".zip"||extension==".vmax"||extension==".jonsbo";}
  async Task Initialize(){try{state.Text="Preparing the official store browser…";
   await FirstRunSetup.InitializeWebViewAsync(browser);browser.CoreWebView2.Settings.AreDevToolsEnabled=false;
   browser.CoreWebView2.Settings.AreHostObjectsAllowed=false;browser.CoreWebView2.Settings.IsWebMessageEnabled=false;
   browser.CoreWebView2.NavigationStarting+=(s,e)=>{
    Uri uri;if(!Uri.TryCreate(e.Uri,UriKind.Absolute,out uri)){e.Cancel=true;return;}
    if(uri.Scheme.Equals("jonsbo-aio",StringComparison.OrdinalIgnoreCase)){
     e.Cancel=true;
     // The website's Import button already checks ownership. Use its authenticated
     // download route instead of launching the vendor app or persisting a token.
     var match=Regex.Match(uri.Query,@"(?:^|[?&])themeId=([0-9]+)(?:&|$)");
     if(trustedPage&&match.Success&&match.Groups[1].Value==themeId)BeginInvoke(new Action(()=>browser.CoreWebView2.Navigate("https://theme.screen-aio.com/Do/downloadTheme/id/"+themeId)));
     return;
    }
    if(uri.Scheme=="https")trustedPage=uri.Host=="theme.screen-aio.com";else{trustedPage=false;if(uri.Scheme!="about")e.Cancel=true;}
   };
   browser.CoreWebView2.NewWindowRequested+=(s,e)=>{e.Handled=true;Uri uri;if(Uri.TryCreate(e.Uri,UriKind.Absolute,out uri)&&uri.Scheme=="https")browser.CoreWebView2.Navigate(uri.AbsoluteUri);};
   browser.CoreWebView2.DownloadStarting+=DownloadStarting;
   state.Text="Official store: theme.screen-aio.com · Waiting for a theme download";
   if(testArchive==null)browser.CoreWebView2.Navigate("https://theme.screen-aio.com/theme/"+themeId+".html");
   else {
    const string testUrl="https://theme.screen-aio.com/studio-download-smoke.html";
    string downloadUrl="https://theme.screen-aio.com/Do/downloadTheme/id/"+themeId;
    browser.CoreWebView2.AddWebResourceRequestedFilter("https://theme.screen-aio.com/*",CoreWebView2WebResourceContext.All);
    browser.CoreWebView2.WebResourceRequested+=(s,e)=>{if(e.Request.Uri==testUrl)e.Response=browser.CoreWebView2.Environment.CreateWebResourceResponse(new MemoryStream(System.Text.Encoding.UTF8.GetBytes("<script>location.href='JONSBO-AIO://open?token=test-only&themeId="+themeId+"'</script>")),200,"OK","Content-Type: text/html");else if(e.Request.Uri==downloadUrl)e.Response=browser.CoreWebView2.Environment.CreateWebResourceResponse(new MemoryStream(testArchive,false),200,"OK","Content-Type: application/octet-stream\r\nContent-Disposition: attachment; filename=studio-download-smoke.vmax");else e.Response=browser.CoreWebView2.Environment.CreateWebResourceResponse(new MemoryStream(),404,"Not found","");};
    browser.CoreWebView2.Navigate(testUrl);
   }
  }catch(Exception){state.Text="The official store could not open. Check your connection and try again.";}}
  void DownloadStarting(object sender,CoreWebView2DownloadStartingEventArgs args){
   var operation=args.DownloadOperation;
   if(complete||downloading||!AllowedDownload(operation.Uri)||!ThemeFile(args.ResultFilePath)){args.Cancel=true;state.Text="Choose a ZIP, VMAX or JONSBO theme download from the official store.";return;}
   args.ResultFilePath=Path.Combine(AppPaths.Data("Downloads"),"community-"+themeId+"-"+Guid.NewGuid().ToString("N")+Path.GetExtension(args.ResultFilePath));
   string target=args.ResultFilePath;downloading=true;state.Text="Downloading theme…";
   operation.BytesReceivedChanged+=(s,e)=>{if(operation.BytesReceived>512L*1024*1024)operation.Cancel();else state.Text="Downloading theme · "+(operation.BytesReceived/1048576.0).ToString("0.0")+" MB";};
   operation.StateChanged+=async(s,e)=>{
    if(operation.State==CoreWebView2DownloadState.Interrupted){downloading=false;state.Text="Download stopped. You can retry from the store.";}
    else if(operation.State==CoreWebView2DownloadState.Completed&&!complete){complete=true;state.Text="Importing and applying theme…";try{await imported(target);downloading=false;Close();}catch(Exception ex){downloading=false;complete=false;state.Text="Import failed: "+ex.Message;}}
   };
  }
  public static void DownloadSmoke(IWin32Window owner,byte[] archive,Func<string,Task> imported){bool received=false;using(var dialog=new CommunityThemeStore("community-0","Download test",async path=>{await imported(path);received=true;},archive))using(var timeout=new Timer{Interval=15000}){timeout.Tick+=(s,e)=>{dialog.downloading=false;dialog.Close();};timeout.Start();dialog.ShowDialog(owner);timeout.Stop();if(!received)throw new Exception("Community store download did not reach the native import callback: "+dialog.state.Text);}}
  protected override void Dispose(bool disposing){if(disposing)browser.Dispose();base.Dispose(disposing);}
 }
}
