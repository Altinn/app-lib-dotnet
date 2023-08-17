# Path: Makefile

build:
	dotnet build
    
executable-osx-x64:
	dotnet publish -c Release -o publish/osx-x64 -r osx-x64 --self-contained

executable-osx-arm64:
	dotnet publish -c Release -o publish/osx-arm64 -r osx-arm64

executable-win-x64:
	dotnet publish -c Release -o publish/win-x64 -r win-x64

executable-linux-x64:
	dotnet publish -c Release -o publish/linux-x64 -r linux-x64
	
executable-linux-arm64:
	dotnet publish -c Release -o publish/linux-arm64 -r linux-arm64
	
executables: executable-osx-x64 executable-osx-arm64 executable-win-x64 executable-linux-x64 executable-linux-arm64