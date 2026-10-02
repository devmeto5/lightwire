# LightWire

A lightweight personal VPN client for Windows 10/11 x64 with an English interface. Uses official WireGuard for Windows for encryption and tunneling. You need your own WireGuard server and a client `.conf` file.

## Installation

1. Install [official WireGuard for Windows](https://www.wireguard.com/install/).
2. Download `LightWire-Setup.exe` from a tagged Release, when available, or the latest successful build artifact in this repository's Actions tab.
3. Run the installer and approve the administrator prompt. It installs to `C:\Program Files\LightWire` and adds a Start menu shortcut.
4. Open LightWire, click **Import .conf**, and select your server's client configuration.
5. Click **Connect**, open a website, and wait for a recent server handshake.

The application and installer are unsigned. Windows may show an unknown publisher warning. Verify the source and SHA256SUMS.txt. Uses built-in .NET Framework 4.x; no Python or .NET SDK is required. Windows-owned dialogs and system error details may follow your Windows language settings.

One profile and one Peer are supported. Disconnect before replacing the profile. Closing the window does not disconnect the VPN; the installed service may also start after reboot. **Disconnect** removes only the LightWirePersonal tunnel service. Avoid running VPNs with conflicting routes.

## Client configuration

Replace all placeholders in angle brackets:

```ini
[Interface]
PrivateKey = <CLIENT_PRIVATE_KEY>
Address = 10.66.66.2/32
DNS = 1.1.1.1

[Peer]
PublicKey = <SERVER_PUBLIC_KEY>
Endpoint = <SERVER_PUBLIC_IP>:51820
AllowedIPs = 0.0.0.0/0, ::/0
PersistentKeepalive = 25
```

`::/0` routes IPv6 into the tunnel. With the IPv4-only server below, IPv6 connectivity will be unavailable while IPv4 continues to work. Removing it may let IPv6 traffic go directly through your ISP. Narrower AllowedIPs enable split tunneling. LightWire has no separate kill switch; WireGuard handles routing and firewall behavior. A running service does not prove internet connectivity: the client separately displays the age of the last handshake.

## Server setup: new Ubuntu VPS

Requires root/sudo, a public IPv4 address, and inbound UDP 51820 allowed in your provider's firewall. This example assumes a new server without an existing firewall configuration. Coordinate existing UFW/nftables/iptables rules with your administrator and keep SSH access available.

```sh
sudo apt update
sudo apt install wireguard iptables
sudo -i
umask 077
mkdir -p /etc/wireguard
cd /etc/wireguard
wg genkey | tee server.key | wg pubkey > server.pub
wg genkey | tee client.key | wg pubkey > client.pub
printf 'net.ipv4.ip_forward=1\n' > /etc/sysctl.d/70-lightwire.conf
sysctl --system
ip route show default
```

Note the external interface after `dev`, such as `ens3`. Create `/etc/wireguard/wg0.conf`, substituting the contents of server.key and client.pub and replacing ens3 with your external interface:

```ini
[Interface]
Address = 10.66.66.1/24
ListenPort = 51820
PrivateKey = <CONTENTS_OF_SERVER.KEY>
PostUp = iptables -A FORWARD -i %i -j ACCEPT; iptables -A FORWARD -o %i -m conntrack --ctstate RELATED,ESTABLISHED -j ACCEPT; iptables -t nat -A POSTROUTING -s 10.66.66.0/24 -o ens3 -j MASQUERADE
PostDown = iptables -D FORWARD -i %i -j ACCEPT; iptables -D FORWARD -o %i -m conntrack --ctstate RELATED,ESTABLISHED -j ACCEPT; iptables -t nat -D POSTROUTING -s 10.66.66.0/24 -o ens3 -j MASQUERADE

[Peer]
PublicKey = <CONTENTS_OF_CLIENT.PUB>
AllowedIPs = 10.66.66.2/32
```

```sh
chmod 600 /etc/wireguard/wg0.conf
systemctl enable --now wg-quick@wg0
wg show
```

Create the client file using the earlier example: PrivateKey from client.key, PublicKey from server.pub, and your VPS address for Endpoint. Transfer it to your PC through a secure channel. Generate a separate key pair and address for every additional device. LightWire does not run these server commands automatically.

## Key storage

Profiles are encrypted with Windows DPAPI in `%ProgramData%\LightWire\LightWirePersonal.conf.dpapi`. Folder access is restricted to Administrators and SYSTEM. Configuration keys are not included in application error messages or uploaded to this repository. The original imported file remains in place: store it securely because it contains a secret. A computer administrator can decrypt the saved profile. Import rejects PreUp/PostUp/PreDown/PostDown commands. WireGuard performs full semantic validation of network settings on connection.

## Building

Run `powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1` on Windows x64. The built-in .NET Framework compiler produces `dist/LightWire.exe`, `dist/LightWire-Setup.exe`, and `dist/SHA256SUMS.txt`. The script also runs configuration validation tests. GitHub Actions repeats the build; a `v*` tag publishes a Release with executables.

## Uninstalling

Click **Disconnect** and close the application. Delete `C:\Program Files\LightWire`, the shortcut `%ProgramData%\Microsoft\Windows\Start Menu\Programs\LightWire.lnk`, and, if no longer needed, `%ProgramData%\LightWire`. Administrator permissions are required. Uninstall WireGuard separately through Windows Settings. This first-version installer does not register an entry in the installed apps list.

## Validation and limitations

Compilation and configuration filtering tests have been checked. A real server connection still needs testing after you provision your VPS. Verify a recent handshake, external IP, DNS, IPv6 behavior, disconnection, reboot behavior, and restoration of normal internet access. This initial version has not had a security audit and does not guarantee bypassing WireGuard blocking.

References: [WireGuard Quick Start](https://www.wireguard.com/quickstart/), [Windows tunnel management](https://git.zx2c4.com/wireguard-windows/about/docs/enterprise.md).
