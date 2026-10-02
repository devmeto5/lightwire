using System;
class Tests {
 static int Main(){string key=Convert.ToBase64String(new byte[32]); string good="[Interface]\nPrivateKey = "+key+"\nAddress = 10.66.66.2/32\nDNS = 1.1.1.1\n[Peer]\nPublicKey = "+key+"\nEndpoint = example.com:51820\nAllowedIPs = 0.0.0.0/0, ::/0\nPersistentKeepalive = 25\n";
  Config.Validate(good); int count=1;
  foreach(string bad in new[]{good+"PostUp = evil",good+"PublicKey = "+key,good.Replace("[Peer]","[Unknown]"),good.Replace("PrivateKey = "+key,"PrivateKey = invalid"),good.Replace("Endpoint = example.com:51820", ""),good+"[Peer]",new string('x',65537)}){bool rejected=false;try{Config.Validate(bad);}catch{rejected=true;}if(!rejected){Console.WriteLine("FAIL");return 1;}count++;}Console.WriteLine("PASS: "+count+" configuration cases");return 0;
 }
}
