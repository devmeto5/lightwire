using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;
class Setup {
 [STAThread] static void Main(){Application.EnableVisualStyles();try{
  string dir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),"LightWire");
  if(MessageBox.Show("Установить LightWire в "+dir+"?\nОфициальный WireGuard устанавливается отдельно.","LightWire Setup",MessageBoxButtons.OKCancel)!=DialogResult.OK)return;
  Directory.CreateDirectory(dir);
  foreach(string name in new[]{"LightWire.exe","README.md"})using(var input=Assembly.GetExecutingAssembly().GetManifestResourceStream(name))using(var output=File.Create(Path.Combine(dir,name)))input.CopyTo(output);
  var shell=Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell")); dynamic shortcut=shell.GetType().InvokeMember("CreateShortcut",BindingFlags.InvokeMethod,null,shell,new object[]{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms),"LightWire.lnk")}); shortcut.TargetPath=Path.Combine(dir,"LightWire.exe"); shortcut.WorkingDirectory=dir; shortcut.Save();
  MessageBox.Show("Готово. Запусти LightWire из меню Пуск.\nИмпортируй конфигурацию своего сервера.\nИнструкция: "+Path.Combine(dir,"README.md"),"LightWire");
 }catch(Exception ex){MessageBox.Show("Установка не завершена: "+ex.Message);Environment.ExitCode=1;}}
}
