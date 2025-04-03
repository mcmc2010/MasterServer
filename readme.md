
```shell
openssl req -x509 -newkey rsa:2048 -keyout private.key -out https.crt -days 365 -nodes -subj "/CN=localhost"
openssl pkcs12 -export -out https.pfx -inkey private.key -in https.crt
dotnet nuget locals all --clear
dotnet restore
dotnet run

```shell
# build win64
dotnet publish MasterServer.csproj -c Release --output ./publish --self-contained true -r win-x64
```

```shell
# -r linux-x86       # x32
# -r linux-x64       # x64
# -r linux-arm       # ARM 32位
# -r linux-arm64     # ARM 64位

# build linux
dotnet publish MasterServer.csproj -c Release --output ./publish-linux --self-contained true -r linux-x64 /p:DefineConstants="LINUX"
dotnet publish MasterServer.csproj -c Release --output ./publish-linux --self-contained true -r linux-x64 /p:DefineConstants="LINUX;LINUX_SERVICE"
```
