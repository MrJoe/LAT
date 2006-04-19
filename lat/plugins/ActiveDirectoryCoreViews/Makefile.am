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

RESOURCES = \
	dialogs.glade \
	$(top_builddir)/resources/users.png \
	$(top_builddir)/resources/contact-new.png \
	$(top_builddir)/resources/stock_person.png \
	$(top_builddir)/resources/x-directory-remote-server-48x48.png

RESOURCES_BUILD = $(addprefix /resource:, $(RESOURCES))

$(ASSEMBLY).dll: $(CSFILES)
	$(CSC) -out:$@ $(CSFILES) $(REFERENCES_BUILD) $(RESOURCES_BUILD) $(GTKSHARP_LIBS)

all: $(ASSEMBLY).dll

ASSEMBLYlibdir = $(prefix)/lib/lat/plugins
ASSEMBLYlib_DATA = $(ASSEMBLY).dll

EXTRA_DIST = \
	$(CSFILES) \
	dialogs.glade

CLEANFILES = \
	$(ASSEMBLY).dll \
	$(ASSEMBLY).dll.mdb
