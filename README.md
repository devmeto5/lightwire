# LightWire

Небольшой персональный VPN-клиент для Windows 10/11 x64, с русским интерфейсом. Использует официальный WireGuard for Windows: собственная реализация криптографии и сетевой драйвер в проект не входят. Для подключения нужен свой сервер WireGuard и его клиентский файл `.conf`.

## Установка

1. Установите [официальный WireGuard](https://www.wireguard.com/install/) для Windows.
2. Скачайте `LightWire-Setup.exe` из Releases или архива сборки Actions этого репозитория.
3. Запустите установщик и подтвердите запрос администратора. Он копирует приложение в `C:\Program Files\LightWire` и добавляет ярлык в меню Пуск.
4. Запустите LightWire, нажмите «Импорт .conf», выберите конфигурацию своего сервера.
5. Нажмите «Подключить», откройте сайт и дождитесь сообщения о свежем рукопожатии с сервером.

Программа и установщик не подписаны коммерческим сертификатом. Windows может показать предупреждение о неизвестном издателе. Проверяйте источник файла и SHA256SUMS.txt. Для работы используется встроенный .NET Framework 4.x; Python и .NET SDK не нужны.

Поддерживается один профиль и один Peer. Заменить профиль можно после отключения. Закрытие окна не отключает VPN; установленная служба может запуститься и после перезагрузки. Кнопка «Отключить» удаляет службу именно LightWirePersonal. Другие туннели программа не изменяет; не включайте одновременно несколько VPN с конфликтующими маршрутами.

## Конфигурация клиента

Файл создаётся при настройке вашего сервера. Пример (значения в угловых скобках обязательно заменить):

```ini
[Interface]
PrivateKey = <ПРИВАТНЫЙ_КЛЮЧ_КЛИЕНТА>
Address = 10.66.66.2/32
DNS = 1.1.1.1

[Peer]
PublicKey = <ПУБЛИЧНЫЙ_КЛЮЧ_СЕРВЕРА>
Endpoint = <ПУБЛИЧНЫЙ_IP_СЕРВЕРА>:51820
AllowedIPs = 0.0.0.0/0, ::/0
PersistentKeepalive = 25
```

`::/0` направляет IPv6 в туннель: на приведённом ниже сервере без IPv6-маршрутизации IPv6 работать не будет, а IPv4 продолжит работать. Удаление `::/0` может позволить IPv6 идти напрямую через провайдера. Частичные AllowedIPs дают split tunnel. Отдельного собственного kill switch приложение не реализует; маршрутизацию и firewall обслуживает WireGuard. Не считайте запуск службы доказательством доступности интернета: клиент отдельно показывает возраст последнего рукопожатия.

## Свой сервер: пример для нового Ubuntu VPS

Требуются root/sudo, публичный IPv4 и разрешённый входящий UDP 51820 в firewall провайдера. Пример предназначен для нового сервера без настроенного firewall; существующие UFW/nftables/iptables правила нужно согласовать с администратором. SSH-порт должен остаться доступным.

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

Запишите имя внешнего интерфейса после `dev` (например `ens3`). Создайте `/etc/wireguard/wg0.conf`, подставив ключ из `server.key`, ключ из `client.pub` и имя интерфейса вместо `ens3`:

```ini
[Interface]
Address = 10.66.66.1/24
ListenPort = 51820
PrivateKey = <СОДЕРЖИМОЕ_SERVER.KEY>
PostUp = iptables -A FORWARD -i %i -j ACCEPT; iptables -A FORWARD -o %i -m conntrack --ctstate RELATED,ESTABLISHED -j ACCEPT; iptables -t nat -A POSTROUTING -s 10.66.66.0/24 -o ens3 -j MASQUERADE
PostDown = iptables -D FORWARD -i %i -j ACCEPT; iptables -D FORWARD -o %i -m conntrack --ctstate RELATED,ESTABLISHED -j ACCEPT; iptables -t nat -D POSTROUTING -s 10.66.66.0/24 -o ens3 -j MASQUERADE

[Peer]
PublicKey = <СОДЕРЖИМОЕ_CLIENT.PUB>
AllowedIPs = 10.66.66.2/32
```

```sh
chmod 600 /etc/wireguard/wg0.conf
systemctl enable --now wg-quick@wg0
wg show
```

Создайте клиентский `.conf` по примеру выше: `PrivateKey` из `client.key`, `PublicKey` из `server.pub`, Endpoint — адрес VPS. Перенесите его на ПК через защищённый канал. Для каждого нового устройства генерируйте отдельную пару ключей и отдельный адрес. Эти команды приложение самостоятельно не выполняет.

## Хранение ключей

Импортированный профиль сохраняется зашифрованным Windows DPAPI в `%ProgramData%\LightWire\LightWirePersonal.conf.dpapi`; доступ к каталогу ограничивается администраторами и SYSTEM. Ключ не попадает в сообщения ошибок или репозиторий. Исходный импортированный `.conf` остаётся на месте: храните его безопасно, он содержит секрет. Администратор компьютера может расшифровать профиль. Импорт не разрешает команды PreUp/PostUp/PreDown/PostDown. Полную семантическую проверку сетевых параметров выполняет WireGuard при подключении.

## Сборка

В Windows x64 выполните `powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1`. Используется встроенный компилятор .NET Framework. Результат — `dist/LightWire.exe`, `dist/LightWire-Setup.exe` и SHA256SUMS.txt. Скрипт также запускает тесты проверки конфигурации. GitHub Actions повторяет сборку; тег `v*` создаёт Release с exe.

## Удаление

Сначала нажмите «Отключить» и закройте приложение. Удалите `C:\Program Files\LightWire`, ярлык `%ProgramData%\Microsoft\Windows\Start Menu\Programs\LightWire.lnk` и, если профиль больше не нужен, `%ProgramData%\LightWire`. Нужны права администратора. WireGuard удаляется отдельно через настройки Windows. Установщик первой версии не регистрируется в списке установленных приложений.

## Проверка и ограничения

Проверены компиляция и тесты фильтрации конфигурации. Подключение к реальному серверу требует отдельной проверки после покупки VPS. Проверить: свежий handshake, внешний IP, DNS, IPv6, отключение, перезагрузку и восстановление обычного интернета. Это первая версия, без аудита безопасности и без гарантии обхода блокировок WireGuard.

Документация: [WireGuard Quick Start](https://www.wireguard.com/quickstart/), [управление туннелями Windows](https://git.zx2c4.com/wireguard-windows/about/docs/enterprise.md).
