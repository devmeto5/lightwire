using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;
class Setup {
 [STAThread] static void Main(){Application.EnableVisualStyles();try{
  string dir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),"LightWire");
  if(MessageBox.Show("Install LightWire to "+dir+"?\nOfficial WireGuard must be installed separately.","LightWire Setup",MessageBoxButtons.OKCancel)!=DialogResult.OK)return;
  Directory.CreateDirectory(dir);
  foreach(string name in new[]{"LightWire.exe","README.md"})using(var input=Assembly.GetExecutingAssembly().GetManifestResourceStream(name))using(var output=File.Create(Path.Combine(dir,name)))input.CopyTo(output);
  var shell=Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell")); dynamic shortcut=shell.GetType().InvokeMember("CreateShortcut",BindingFlags.InvokeMethod,null,shell,new object[]{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms),"LightWire.lnk")}); shortcut.TargetPath=Path.Combine(dir,"LightWire.exe"); shortcut.WorkingDirectory=dir; shortcut.Save();
  MessageBox.Show("Setup complete. Open LightWire from the Start menu.\nImport your server configuration.\nInstructions: "+Path.Combine(dir,"README.md"),"LightWire");
 }catch(Exception ex){MessageBox.Show("Setup could not be completed: "+ex.Message);Environment.ExitCode=1;}}
}
