configuration = Release

restore:
	dotnet tool restore

win-publish:
	dotnet publish src/Runner/Runner.csproj --self-contained true -p:PublishSingleFile=true --runtime win-x64 --configuration $(configuration) --output publish/win-x64
	dotnet publish src/Runner/Runner.csproj --self-contained true -p:PublishSingleFile=true --runtime win-arm64 --configuration $(configuration) --output publish/win-arm64

osx-publish:
	dotnet publish src/Runner/Runner.csproj --self-contained true -p:PublishSingleFile=true --runtime osx-x64 --configuration $(configuration) --output publish/osx-x64
	dotnet publish src/Runner/Runner.csproj --self-contained true -p:PublishSingleFile=true --runtime osx-arm64 --configuration $(configuration) --output publish/osx-arm64

linux-publish:
	dotnet publish src/Runner/Runner.csproj --self-contained true -p:PublishSingleFile=true --runtime linux-x64 --configuration $(configuration) --output publish/linux-x64
	dotnet publish src/Runner/Runner.csproj --self-contained true -p:PublishSingleFile=true --runtime linux-arm64 --configuration $(configuration) --output publish/linux-arm64

publish: restore win-publish osx-publish linux-publish