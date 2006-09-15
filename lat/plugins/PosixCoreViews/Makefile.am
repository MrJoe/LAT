CSC = $(MCS) -codepage:utf8 -target:library $(MCS_FLAGS)

ASSEMBLY = PosixCoreViews

CSFILES = \
	EditContactsViewDialog.cs \
	EditUserViewDialog.cs \
	GroupsViewDialog.cs \
	HostsViewDialog.cs \
	NewContactsViewDialog.cs \
	NewUserViewDialog.cs \
	PosixComputerViewPlugin.cs \
	PosixContactsViewPlugin.cs \
	PosixGroupViewPlugin.cs \
	PosixUserViewPlugin.cs \
	UserDefaultValuesDialog.cs

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
	dialogs.glade \
	$(top_builddir)/resources/contact-new-48x48.png \
	$(top_builddir)/resources/x-directory-remote-server-48x48.png \
	$(top_builddir)/resources/x-directory-remote-workgroup.png \
	$(top_builddir)/resources/contact-new.png \
	$(top_builddir)/resources/users.png \
	$(top_builddir)/resources/locked16x16.png \
	$(top_builddir)/resources/stock_person.png

RESOURCES_BUILD = $(addprefix /resource:, $(RESOURCES))

$(ASSEMBLY).dll: $(CSFILES)
	$(CSC) -out:$@ $(CSFILES) $(REFERENCES_BUILD) $(RESOURCES_BUILD) $(GTKSHARP_LIBS)

all: $(ASSEMBLY).dll

ASSEMBLYlibdir = $(pkglibdir)/plugins
ASSEMBLYlib_DATA = $(ASSEMBLY).dll

EXTRA_DIST = \
	$(CSFILES) \
	dialogs.glade

CLEANFILES = \
	$(ASSEMBLY).dll \
	$(ASSEMBLY).dll.mdb
