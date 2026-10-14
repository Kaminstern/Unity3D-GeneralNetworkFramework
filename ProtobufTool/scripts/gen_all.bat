@echo off
set ROOT=%~dp0..
set PROTOC=%ROOT%\tools\protoc\bin\protoc.exe
set PROTO_DIR=%ROOT%\protos
set SERVER_OUT=%ROOT%\..\GeneralServerFramework\src\Generated
set CLIENT_OUT=%ROOT%\..\GeneralClientNetworkFramework\Assets\Scripts\Generated

if not exist "%SERVER_OUT%" mkdir "%SERVER_OUT%"
if not exist "%CLIENT_OUT%" mkdir "%CLIENT_OUT%"

%PROTOC% --proto_path="%PROTO_DIR%" --csharp_out="%SERVER_OUT%" "%PROTO_DIR%\*.proto"
%PROTOC% --proto_path="%PROTO_DIR%" --csharp_out="%CLIENT_OUT%" "%PROTO_DIR%\*.proto"

echo success
pause