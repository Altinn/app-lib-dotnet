NUGET_RELEASE_FOLDER ?= bin\\Release
NUGET_FEED ?= ~\\.nuget-local\\
NUGET_VERSION ?= 8.0.0
NUGET_VERSION_SUFFIX ?= -local.
NUGET_BUILD_NUMBER ?= 0

ifeq ($(OS),Windows_NT)
    Remove = if exist $(1) rmdir /s /q $(1)
else
    Remove = rm -rf $(1)
endif

.PHONY: pack
pack:
	$(call Remove,$(NUGET_RELEASE_FOLDER))
	dotnet pack AppLibDotnet.sln -p:MinVerVersionOverride=$(NUGET_VERSION)$(NUGET_VERSION_SUFFIX)$(NUGET_BUILD_NUMBER) -o $(NUGET_RELEASE_FOLDER)
	 
.PHONY: publish
publish: pack
	nuget init $(NUGET_RELEASE_FOLDER) $(NUGET_FEED)
	echo "New version available: $(NUGET_VERSION)$(NUGET_VERSION_SUFFIX)$(NUGET_BUILD_NUMBER)"
	 
.PHONY: clean
clean:
	dotnet clean AppLibDotnet.sln
	$(call Remove,bin/Release)

.PHONY: clear
clear: clean
	dotnet nuget locals all --clear
	$(call Remove,$(NUGET_FEED))
