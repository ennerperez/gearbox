.PHONY: * #since no targets will produce files, saves us from needing to specify on all https://www.gnu.org/software/make/manual/html_node/Phony-Targets.html

environment = Test
configuration = Release
nukeproject = "build\_build.csproj"

# DOTNET #

restore:
	dotnet restore

win-publish-x64:
	dotnet publish src/Host/Host.csproj --self-contained true -p:PublishSingleFile=true --runtime win-x64 --configuration $(configuration) --output publish/win-x64
	dotnet publish src/Runner/Runner.csproj --self-contained true -p:PublishSingleFile=true --runtime win-x64 --configuration $(configuration) --output publish/win-x64
	dotnet publish src/Shell/Shell.csproj --self-contained true -p:PublishSingleFile=true --runtime win-x64 --configuration $(configuration) --output publish/win-x64

win-publish-arm:
	dotnet publish src/Host/Host.csproj --self-contained true -p:PublishSingleFile=true --runtime win-arm64 --configuration $(configuration) --output publish/win-arm64
	dotnet publish src/Runner/Runner.csproj --self-contained true -p:PublishSingleFile=true --runtime win-arm64 --configuration $(configuration) --output publish/win-arm64
	dotnet publish src/Shell/Shell.csproj --self-contained true -p:PublishSingleFile=true --runtime win-arm64 --configuration $(configuration) --output publish/win-arm64

win-publish: win-publish-x64 win-publish-arm

osx-publish-x64:
	dotnet publish src/Host/Host.csproj --self-contained true -p:PublishSingleFile=true --runtime osx-x64 --configuration $(configuration) --output publish/osx-x64
	dotnet publish src/Runner/Runner.csproj --self-contained true -p:PublishSingleFile=true --runtime osx-x64 --configuration $(configuration) --output publish/osx-x64
	dotnet publish src/Shell/Shell.csproj --self-contained true -p:PublishSingleFile=true --runtime osx-x64 --configuration $(configuration) --output publish/osx-x64

osx-publish-arm:
	dotnet publish src/Host/Host.csproj --self-contained true -p:PublishSingleFile=true --runtime osx-arm64 --configuration $(configuration) --output publish/osx-arm64
	dotnet publish src/Runner/Runner.csproj --self-contained true -p:PublishSingleFile=true --runtime osx-arm64 --configuration $(configuration) --output publish/osx-arm64
	dotnet publish src/Shell/Shell.csproj --self-contained true -p:PublishSingleFile=true --runtime osx-arm64 --configuration $(configuration) --output publish/osx-arm64

osx-publish: osx-publish-x64 osx-publish-arm

linux-publish-x64:
	dotnet publish src/Host/Host.csproj --self-contained true -p:PublishSingleFile=true --runtime linux-x64 --configuration $(configuration) --output publish/linux-x64
	dotnet publish src/Runner/Runner.csproj --self-contained true -p:PublishSingleFile=true --runtime linux-x64 --configuration $(configuration) --output publish/linux-x64
	dotnet publish src/Shell/Shell.csproj --self-contained true -p:PublishSingleFile=true --runtime linux-x64 --configuration $(configuration) --output publish/linux-x64

linux-publish-arm:
	dotnet publish src/Host/Host.csproj --self-contained true -p:PublishSingleFile=true --runtime linux-arm64 --configuration $(configuration) --output publish/linux-arm64
	dotnet publish src/Runner/Runner.csproj --self-contained true -p:PublishSingleFile=true --runtime linux-arm64 --configuration $(configuration) --output publish/linux-arm64
	dotnet publish src/Shell/Shell.csproj --self-contained true -p:PublishSingleFile=true --runtime linux-arm64 --configuration $(configuration) --output publish/linux-arm64

linux-publish: linux-publish-x64 linux-publish-arm

publish-x64: restore win-publish-x64 osx-publish-x64 linux-publish-x64

publish-arm: restore win-publish-arm osx-publish-arm linux-publish-arm

publish: restore publish-x64 publish-arm

# NUKE BUILD #

nuke:
	dotnet build ${nukeproject} /nodeReuse:false /p:UseSharedCompilation=false -nologo -clp:NoSummary --verbosity quiet

nuke-clean: nuke
	dotnet run --project ${nukeproject} --no-build -- --target Clean

nuke-prepare: nuke
	dotnet run --project ${nukeproject} --no-build -- --target Prepare

nuke-versioning: nuke
	dotnet run --project ${nukeproject} --no-build -- --target Versioning

nuke-restore: nuke
	dotnet run --project ${nukeproject} --no-build -- --target Restore

nuke-compile: nuke
	dotnet run --project ${nukeproject} --no-build -- --target Compile

nuke-publish: nuke
	dotnet run --project ${nukeproject} --no-build -- --target Publish --configuration Release --environment $environment

nuke-pack: nuke
	dotnet run --project ${nukeproject} --no-build -- --target Pack

nuke-test-unittest: nuke
	dotnet run --project ${nukeproject} --no-build -- --target UnitTest

nuke-test-uitest: nuke
	dotnet run --project ${nukeproject} --no-build -- --target UITest

nuke-analyze: nuke
	dotnet run --project ${nukeproject} --no-build -- --target Analyze