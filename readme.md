
```shell
openssl req -x509 -newkey rsa:2048 -keyout private.key -out https.crt -days 365 -nodes -subj "/CN=localhost"
openssl pkcs12 -export -out https.pfx -inkey private.key -in https.crt
dotnet nuget locals all --clear
dotnet restore
dotnet run
dotnet publish -c Release -o ./publish --self-contained true -r win-x64
```
