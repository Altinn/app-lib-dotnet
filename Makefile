NUGET_RELEASE_FOLDER ?= bin/Release
NUGET_FEED ?= ~/.nuget-local/
NUGET_VERSION ?= 8.0.0
NUGET_VERSION_SUFFIX ?= -local.
NUGET_BUILD_NUMBER ?= 0

@PHONY: pack
pack:
	rm -rf $(NUGET_RELEASE_FOLDER)/*
	dotnet pack AppLibDotnet.sln -p:MinVerVersionOverride=$(NUGET_VERSION)$(NUGET_VERSION_SUFFIX)$(NUGET_BUILD_NUMBER) -o $(NUGET_RELEASE_FOLDER)
	 
@PHONY: publish
publish: pack
	nuget init $(NUGET_RELEASE_FOLDER) $(NUGET_FEED)
	echo "New version available: $(NUGET_VERSION)$(NUGET_VERSION_SUFFIX)$(NUGET_BUILD_NUMBER)"
	 
@PHONY: clean
clean:
	dotnet clean AppLibDotnet.sln
	rm -rf bin/Release

@PHONY: clear
clear: clean
	dotnet nuget locals all --clear
	rm -rf $(NUGET_FEED)/*

