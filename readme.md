
### 说明
- 缺少一些数据文件和类，这些根据自己项目生成或编辑，此处不再公开。

### Build
```shell
openssl req -x509 -newkey rsa:2048 -keyout private.key -out https.crt -days 365 -nodes -subj "/CN=localhost"
openssl pkcs12 -export -out https.pfx -inkey private.key -in https.crt
dotnet nuget locals all --clear
dotnet restore
dotnet run
```

### 创建CA证书

```shell
# openssl new version:
openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048 -pass pass:123456 -out ./certs/ca_rsa_2048.pem
openssl req -new -x509 -days 365 -key ./certs/ca_rsa_2048.pem -out ./certs/ca.crt -subj "/C=CN/ST=SH/L=SH/O=COM/OU=AM/CN=CA/emailAddress=admin@mcmcx.com" -passout pass:123456
```

### 创建Database证书

```shell
openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048 -pass pass:123456 -out ./certs/db_rsa_2048.pem
openssl req -new -key ./certs/db_rsa_2048.pem -passin pass:123456 -out ./certs/db.csr -subj "/C=CN/ST=SH/L=SH/O=COM/OU=AM/CN=HTTPS/emailAddress=admin@mcmcx.com"
openssl x509 -req -days 365 -in ./certs/db.csr -CA ./certs/ca.crt -CAkey ./certs/ca_rsa_2048.pem -passin pass:123456 -CAcreateserial -out ./certs/db.crt 
# 去除自定义证书私钥的密码
openssl rsa -in ./certs/db_rsa_2048.pem -out ./certs/db_rsa_2048.pem.unsecure
```

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


### Protobufs generated
```shell
./tools/google/bin/protoc --proto_path="./data/protocols" --csharp_out="./src/protocals/generated" "./data/protocols/chat.proto"
```