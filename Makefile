.PHONY: build-osx watch

build-osx:
	dotnet publish TerminalCommands/TerminalCommands.csproj \
		-c Release \
		--runtime osx-x64 \
		--self-contained true \
		-p:PublishSingleFile=true \
		-p:AssemblyName=KallehaugeTerminalCommands-osx-x64 \
		-o ./publish/osx-x64

watch:
	dotnet watch --project TerminalCommands/TerminalCommands.csproj run
