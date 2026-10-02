using System;
using System.IO;
using System.Drawing;
using System.Diagnostics;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Security.Cryptography;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
class MainForm:Form {
 const string Tunnel="LightWirePersonal", Service="WireGuardTunnel$LightWirePersonal";
 static readonly string Folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),"LightWire");
 static readonly string Profile=Path.Combine(Folder,Tunnel+".conf.dpapi");
 static readonly string Engine=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),"WireGuard","wireguard.exe");
 Label status=new Label(); Button import=new Button(),connect=new Button(),disconnect=new Button(); bool busy;
 [STAThread] static void Main(){Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false); bool created; using(var mutex=new System.Threading.Mutex(true,"Global\\LightWirePersonalUI",out created)){if(!created)return; Application.Run(new MainForm());}}
 public MainForm(){
  Text="LightWire • Personal VPN"; ClientSize=new Size(560,370); FormBorderStyle=FormBorderStyle.FixedSingle; MaximizeBox=false; StartPosition=FormStartPosition.CenterScreen; Font=new Font("Segoe UI",11); BackColor=Color.FromArgb(245,247,251);
  var title=new Label{Text="LightWire",Font=new Font("Segoe UI",25,FontStyle.Bold),Location=new Point(28,20),AutoSize=true}; Controls.Add(title);
  status.SetBounds(30,85,500,85); Controls.Add(status);
  import.Text="Import .conf"; import.SetBounds(30,180,155,42); import.Click+=async(s,e)=>await Act(Import); Controls.Add(import);
  connect.Text="Connect"; connect.SetBounds(200,180,155,42); connect.Click+=async(s,e)=>await Act(Connect); Controls.Add(connect);
  disconnect.Text="Disconnect"; disconnect.SetBounds(370,180,155,42); disconnect.Click+=async(s,e)=>await Act(Disconnect); Controls.Add(disconnect);
  var link=new LinkLabel{Text="Install official WireGuard",AutoSize=true,Location=new Point(30,245)}; link.LinkClicked+=(s,e)=>Process.Start("https://www.wireguard.com/install/"); Controls.Add(link);
  Controls.Add(new Label{Text="The tunnel stays active when you close this window.\nClick Disconnect to stop it.",Location=new Point(30,285),Size=new Size(490,55)});
  var timer=new Timer{Interval=5000}; timer.Tick+=async(s,e)=>{if(!busy)await Act(RefreshStatus);}; timer.Start(); Shown+=async(s,e)=>await Act(RefreshStatus); FormClosed+=(s,e)=>timer.Dispose();
 }
 async Task Act(Func<Task> action){ if(busy)return; busy=true; import.Enabled=connect.Enabled=disconnect.Enabled=false; try{await action();}catch(Exception ex){MessageBox.Show(ex.Message,"LightWire",MessageBoxButtons.OK,MessageBoxIcon.Warning);}finally{busy=false;import.Enabled=connect.Enabled=disconnect.Enabled=true;} }
 static bool Running(){try{using(var s=new ServiceController(Service)){return s.Status!=ServiceControllerStatus.Stopped;}}catch(InvalidOperationException){return false;}}
 static async Task<string> Run(string exe,string args){
  var psi=new ProcessStartInfo(exe,args){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};
  using(var p=Process.Start(psi)){var stdout=p.StandardOutput.ReadToEndAsync(); var stderr=p.StandardError.ReadToEndAsync(); bool done=await Task.Run(()=>p.WaitForExit(15000)); if(!done)throw new Exception("This operation is taking longer than expected. Check the status before retrying."); string result=await stdout; await stderr; if(p.ExitCode!=0)throw new Exception("WireGuard could not complete the operation (code "+p.ExitCode+"). Check the configuration in the official client."); return result;}
 }
 async Task Import(){if(Running())throw new Exception("Disconnect the tunnel first."); using(var dialog=new OpenFileDialog{Filter="WireGuard (*.conf)|*.conf",CheckFileExists=true}){if(dialog.ShowDialog()!=DialogResult.OK)return;
  if(new FileInfo(dialog.FileName).Length>65536)throw new Exception("The file is too large."); string config=Config.Validate(File.ReadAllText(dialog.FileName));
  if(File.Exists(Profile)&&MessageBox.Show("Replace the saved profile?","LightWire",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;
  Directory.CreateDirectory(Folder); if((File.GetAttributes(Folder)&FileAttributes.ReparsePoint)!=0)throw new Exception("The profile folder must not be a symbolic link or junction.");
  var acl=new DirectorySecurity(); acl.SetAccessRuleProtection(true,false); foreach(var sid in new[]{WellKnownSidType.BuiltinAdministratorsSid,WellKnownSidType.LocalSystemSid})acl.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(sid,null),FileSystemRights.FullControl,InheritanceFlags.ContainerInherit|InheritanceFlags.ObjectInherit,PropagationFlags.None,AccessControlType.Allow)); Directory.SetAccessControl(Folder,acl);
  byte[] plain=Encoding.UTF8.GetBytes(config); try{var encrypted=ProtectedData.Protect(plain,null,DataProtectionScope.LocalMachine); string temp=Path.Combine(Folder,Guid.NewGuid()+".tmp"); File.WriteAllBytes(temp,encrypted); try{if(File.Exists(Profile))File.Replace(temp,Profile,null);else File.Move(temp,Profile);}finally{if(File.Exists(temp))File.Delete(temp);}}finally{Array.Clear(plain,0,plain.Length);}
 } await RefreshStatus(); }
 async Task Connect(){if(!File.Exists(Engine))throw new Exception("Install official WireGuard using the link in this window first.");if(!File.Exists(Profile))throw new Exception("Import a .conf file first."); if(!Running()){bool missing=false;try{using(var s=new ServiceController(Service)){var unused=s.Status; s.Start();}}catch(InvalidOperationException){missing=true;} if(missing)await Run(Engine,"/installtunnelservice \""+Profile+"\"");}await RefreshStatus();}
 async Task Disconnect(){if(!File.Exists(Engine))throw new Exception("WireGuard was not found. Stop the service using services.msc."); bool exists=true;try{using(var s=new ServiceController(Service)){var unused=s.Status;}}catch(InvalidOperationException){exists=false;}if(exists)await Run(Engine,"/uninstalltunnelservice "+Tunnel);await RefreshStatus();}
 async Task RefreshStatus(){if(!Running()){status.Text=File.Exists(Profile)?"Disconnected\nYour profile is saved on this computer.":"No profile\nImport the .conf file from your server.";return;}
  status.Text="Tunnel started; waiting for a confirmed server response."; string wg=Path.Combine(Path.GetDirectoryName(Engine),"wg.exe"); if(!File.Exists(wg))return;
  string result=await Run(wg,"show "+Tunnel+" latest-handshakes"); long latest=0; foreach(var line in result.Split('\n')){var parts=line.Trim().Split(new[]{'\t',' '},StringSplitOptions.RemoveEmptyEntries);long t;if(parts.Length==2&&long.TryParse(parts[1],out t))latest=Math.Max(latest,t);}
  long now=(long)(DateTime.UtcNow-new DateTime(1970,1,1)).TotalSeconds; status.Text=latest>0&&now-latest<180?"Server responded • tunnel active\nLast handshake: "+Math.Max(0,now-latest)+" seconds ago.":"Tunnel started; no recent server response.\nOpen a website to test it, or check your server.";
 }
}

