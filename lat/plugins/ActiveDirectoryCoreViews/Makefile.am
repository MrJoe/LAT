CSC = $(MCS) -codepage:utf8 -target:library $(MCS_FLAGS)

ASSEMBLY = ActiveDirectoryCoreViews

CSFILES = \
	ActiveDirectoryComputerViewPlugin.cs \
	ActiveDirectoryContactsViewPlugin.cs \
	ActiveDirectoryGroupViewPlugin.cs \
	ActiveDirectoryUserViewPlugin.cs \
	EditAdComputerViewDialog.cs \
	EditAdUserViewDialog.cs \
	EditContactsViewDialog.cs \
	GroupsViewDialog.cs \
	NewAdComputerViewDialog.cs \
	NewAdUserViewDialog.cs \
	NewContactsViewDialog.cs

SOURCES_BUILD = $(addprefix $(srcdir)/, $(CSFILES))

#if ENABLE_NETWORKMANAGER
NM_REFERENCES = \
	$(top_builddir)/network-manager/network-manager.dll
#endif

REFERENCES = \
	Mono.Posix \
	Mono.Security \
	Novell.Directory.Ldap \
	$(NM_REFERENCES) \
	$(top_builddir)/lat/lat.exe

REFERENCES_BUILD = $(addprefix -r:, $(REFERENCES))

RESOURCES = \
	$(srcdir)/dialogs.glade \
	$(top_srcdir)/resources/users.png \
	$(top_srcdir)/resources/contact-new.png \
	$(top_srcdir)/resources/contact-new-48x48.png \
	$(top_srcdir)/resources/stock_person.png \
	$(top_srcdir)/resources/x-directory-remote-server-48x48.png

RESOURCES_BUILD = $(addprefix /resource:, $(RESOURCES))

$(ASSEMBLY).dll: $(SOURCES_BUILD)
	$(CSC) -out:$@ $(SOURCES_BUILD) $(REFERENCES_BUILD) $(RESOURCES_BUILD) $(GTKSHARP_LIBS)

all: $(ASSEMBLY).dll

ASSEMBLYlibdir = $(pkglibdir)/plugins
ASSEMBLYlib_DATA = $(ASSEMBLY).dll

EXTRA_DIST = \
	$(CSFILES) \
	dialogs.glade

CLEANFILES = \
	$(ASSEMBLY).dll \
	$(ASSEMBLY).dll.mdb
