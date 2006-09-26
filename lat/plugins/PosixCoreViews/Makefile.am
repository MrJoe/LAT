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

if BUILD_AVAHI
AVAHI_REFERENCES = $(AVAHI_LIBS)
endif

if BUILD_NETWORKMANAGER
NM_REFERENCES = \
	$(top_builddir)/network-manager/network-manager.dll
endif

REFERENCES = \
	Mono.Posix \
	Mono.Security \
	Novell.Directory.Ldap \
	$(NM_REFERENCES) \
	$(top_builddir)/lat/lat.exe

REFERENCES_BUILD = $(addprefix -r:, $(REFERENCES))

RESOURCES = \
	$(srcdir)/dialogs.glade \
	$(top_srcdir)/resources/contact-new-48x48.png \
	$(top_srcdir)/resources/x-directory-remote-server-48x48.png \
	$(top_srcdir)/resources/x-directory-remote-workgroup.png \
	$(top_srcdir)/resources/contact-new.png \
	$(top_srcdir)/resources/users.png \
	$(top_srcdir)/resources/locked16x16.png \
	$(top_srcdir)/resources/stock_person.png

RESOURCES_BUILD = $(addprefix /resource:, $(RESOURCES))

$(ASSEMBLY).dll: $(SOURCES_BUILD)
	$(CSC) -out:$@ $(SOURCES_BUILD) $(REFERENCES_BUILD) $(AVAHI_REFERENCES) $(RESOURCES_BUILD) $(GTKSHARP_LIBS)

all: $(ASSEMBLY).dll

ASSEMBLYlibdir = $(pkglibdir)/plugins
ASSEMBLYlib_DATA = $(ASSEMBLY).dll

EXTRA_DIST = \
	$(CSFILES) \
	dialogs.glade

CLEANFILES = \
	$(ASSEMBLY).dll \
	$(ASSEMBLY).dll.mdb
