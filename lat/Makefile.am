SUBDIRS = . plugins

if ENABLE_AVAHI
AVAHI_CSFLAGS = -define:ENABLE_AVAHI
AVAHI_CSFILES = \
	$(srcdir)/ServiceFinder.cs
endif

if ENABLE_NETWORKMANAGER
NETWORKMANAGER_CSFLAGS = -define:ENABLE_NETWORKMANAGER
endif

CSC = $(MCS) -codepage:utf8 $(MCS_FLAGS) $(AVAHI_CSFLAGS) $(NETWORKMANAGER_CSFLAGS)

ASSEMBLY = lat

CSFILES = \
	AboutDialog.cs \
	AddObjectClassDialog.cs \
	AssemblyInfo.cs \
	AttributeEditorWidget.cs \
	ConnectDialog.cs \
	ConnectionManager.cs \
	CreateEntryDialog.cs \
	Defines.cs \
	LDIF.cs \
	LdapEntryAnalyzer.cs \
	LdapSearch.cs \
	LdapServer.cs \
	LdapTreeView.cs \
	Log.cs \
	LoginDialog.cs \
	Main.cs \
	MassEditDialog.cs \
	NewEntryDialog.cs \
	PasswordDialog.cs \
	Preferences.cs \
	ProfileDialog.cs \
	RenameEntryDialog.cs \
	SMBPassword.cs \
	SambaPopulateDialog.cs \
	SchemaTreeView.cs \
	SearchBuilderDialog.cs \
	SearchResultsTreeView.cs \
	SelectContainerDialog.cs \
	SelectGroupsDialog.cs \
	ServerData.cs \
	Templates.cs \
	TemplatesDialog.cs \
	TemplateEditorDialog.cs \
	TimeDateDialog.cs \
	Util.cs \
	ViewDataTreeView.cs \
	ViewDialog.cs \
	ViewPluginManager.cs \
	ViewsTreeView.cs \
	Window.cs

SOURCES_BUILD = $(addprefix $(srcdir)/, $(CSFILES))

REFERENCES = \
	../gnome-keyring-glue/gnome-keyring-glue.dll \
	../network-manager/network-manager.dll \
	Mono.Posix \
	Mono.Security \
	Novell.Directory.Ldap

REFERENCES_BUILD = $(addprefix -r:, $(REFERENCES))

RESOURCES = \
	lat.glade \
	lat.png \
	contact-new.png \
	contact-new-48x48.png \
	edit-find.png \
	edit-find-48x48.png \
	locked16x16.png \
	locked-48x48.png \
	stock_person.png \
	text-x-generic.png \
	unlocked16x16.png \
	users.png \
	x-directory-normal.png \
	x-directory-remote-server.png \
	x-directory-remote-workgroup.png \
	mail-message-new.png \
	document-save.png \
	go-home.png \
	x-directory-remote-server-48x48.png

RESOURCES_BUILD = $(addprefix /resource:$(top_srcdir)/resources/, $(RESOURCES))

$(ASSEMBLY).exe: $(SOURCES_BUILD) $(AVAHI_CSFILES)
	$(CSC) -out:$@ $(SOURCES_BUILD) $(AVAHI_CSFILES) $(REFERENCES_BUILD) $(RESOURCES_BUILD) $(GTKSHARP_LIBS) $(AVAHI_LIBS)

all: $(ASSEMBLY).exe

ASSEMBLYlibdir = $(pkglibdir)
ASSEMBLYlib_DATA = $(ASSEMBLY).exe

man_MANS = \
	$(ASSEMBLY).1

bin_SCRIPTS = $(ASSEMBLY)

EXTRA_DIST = \
	$(CSFILES) \
	ServiceFinder.cs \
	$(man_MANS) \
	AssemblyInfo.cs.in \
	Defines.cs.in \
	$(ASSEMBLY).in

CLEANFILES = \
	$(ASSEMBLY).exe \
	$(ASSEMBLY).exe.mdb

DISTCLEANFILES = \
	AssemblyInfo.cs \
	Defines.cs \
	$(ASSEMBLY)
