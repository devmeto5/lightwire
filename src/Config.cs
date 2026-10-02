using System;
using System.Collections.Generic;
using System.Text;
public static class Config {
 public static string Validate(string text) {
  if (text.Length > 65536) throw new Exception("Конфигурация слишком большая.");
  string section=""; bool iface=false, peer=false; var seen=new HashSet<string>(); var output=new StringBuilder();
  foreach(var raw in text.Split('\n')) {
   string line=raw.Split('#')[0].Trim(); if(line.Length==0) continue;
   if(line=="[Interface]" || line=="[Peer]") {
    if(line=="[Interface]") { if(iface || peer) throw new Exception("Допускается один Interface перед Peer."); iface=true; section="I"; }
    else { if(!iface || peer) throw new Exception("Версия 1 поддерживает один сервер (Peer)."); peer=true; section="P"; }
    output.AppendLine(line); continue;
   }
   int eq=line.IndexOf('='); if(eq<1 || section=="") throw new Exception("Неверный формат конфигурации.");
   string key=line.Substring(0,eq).Trim(), value=line.Substring(eq+1).Trim();
   string allowed=section=="I"?"|PrivateKey|Address|DNS|MTU|ListenPort|":"|PublicKey|PresharedKey|Endpoint|AllowedIPs|PersistentKeepalive|";
   if(!allowed.Contains("|"+key+"|") || !seen.Add(section+key) || value.Length==0) throw new Exception("Неизвестное, повторное или пустое поле: "+key);
   if(key.EndsWith("Key")) { byte[] bytes; try { bytes=Convert.FromBase64String(value); } catch { throw new Exception("Неверный формат ключа."); } if(bytes.Length!=32) throw new Exception("Ключ должен содержать 32 байта."); }
   output.AppendLine(key+" = "+value);
  }
  foreach(string required in new[]{"IPrivateKey","IAddress","PPublicKey","PEndpoint","PAllowedIPs"}) if(!seen.Contains(required)) throw new Exception("Отсутствует обязательное поле: "+required.Substring(1));
  return output.ToString();
 }
}
