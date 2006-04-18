CSC = $(MCS) -codepage:utf8 -target:library $(MCS_FLAGS)

ASSEMBLY = ActiveDirectoryCoreViews

CSFILES = \
	ActiveDirectoryComputerViewPlugin.cs \
	ActiveDirectoryContactsViewPlugin.cs \
	ActiveDirectoryGroupViewPlugin.cs \
	ActiveDirectoryUserViewPlugin.cs \
	EditAdComputerViewDialog.cs \
	NewAdComputerViewDialog.cs \
	NewAdGroupViewDialog.cs \
	NewAdUserViewDialog.cs\ 
	adUserViewDialog.cs

SOURCES_BUILD = $(addprefix $(srcdir)/, $(CSFILES))

REFERENCES = \
	Mono.Posix \
	Mono.Security \
	Novell.Directory.Ldap \
	$(top_builddir)/lat/lat.exe

REFERENCES_BUILD = $(addprefix -r:, $(REFERENCES))

$(ASSEMBLY).dll: $(CSFILES)
	$(CSC) -out:$@ $(CSFILES) $(REFERENCES_BUILD) $(GTKSHARP_LIBS)

all: $(ASSEMBLY).dll

ASSEMBLYlibdir = $(prefix)/lib/lat/plugins
ASSEMBLYlib_DATA = $(ASSEMBLY).dll

EXTRA_DIST = \
	$(CSFILES)

CLEANFILES = \
	$(ASSEMBLY).dll \
	$(ASSEMBLY).dll.mdb
