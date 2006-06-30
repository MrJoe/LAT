SUBDIRS = . plugins

if ENABLE_AVAHI
AVAHI_CSFLAGS = -define:ENABLE_AVAHI
AVAHI_CSFILES = \
	ServiceFinder.cs
endif

CSC = $(MCS) -codepage:utf8 $(MCS_FLAGS) $(AVAHI_CSFLAGS)

ASSEMBLY = lat

NETWORKMANAGER_SOURCES = \
	NetworkManager/Manager.cs \
	NetworkManager/Device.cs \
	NetworkManager/Network.cs 

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
	LdapSearch.cs \
	LdapServer.cs \
	LdapTreeView.cs \
	Logger.cs \
	LoginDialog.cs \
	Main.cs \
	MassEditDialog.cs \
	NetworkDetect.cs \
	NewEntryDialog.cs \
	PasswordDialog.cs \
	Preferences.cs \
	ProfileDialog.cs \
	ProfileManager.cs \
	RenameEntryDialog.cs \
	SMBPassword.cs \
	SambaPopulateDialog.cs \
	SchemaTreeView.cs \
	SearchBuilderDialog.cs \
	SearchResultsTreeView.cs \
	SelectContainerDialog.cs \
	SelectGroupsDialog.cs \
	ServerView.cs \
	Templates.cs \
	TemplatesDialog.cs \
	TemplateEditorDialog.cs \
	TimeDateDialog.cs \
	Util.cs \
	ViewDataTreeView.cs \
	ViewDialog.cs \
	ViewPluginManager.cs \
	ViewsTreeView.cs \
	Window.cs \
	$(NETWORKMANAGER_SOURCES)

SOURCES_BUILD = $(addprefix $(srcdir)/, $(CSFILES))

REFERENCES = \
	../gnome-keyring-glue/gnome-keyring-glue.dll \
	Mono.Posix \
	Mono.Security \
	Novell.Directory.Ldap

REFERENCES_BUILD = $(addprefix -r:, $(REFERENCES))

$(ASSEMBLY).exe: $(CSFILES) $(AVAHI_CSFILES)
	$(CSC) -out:$@ $(CSFILES) $(AVAHI_CSFILES) $(REFERENCES_BUILD) @lat.resources $(GTKSHARP_LIBS) $(DBUS_LIBS) $(AVAHI_LIBS)

all: $(ASSEMBLY).exe

ASSEMBLYlibdir = $(pkglibdir)
ASSEMBLYlib_DATA = $(ASSEMBLY).exe

man_MANS = \
	$(ASSEMBLY).1

bin_SCRIPTS = $(ASSEMBLY)

EXTRA_DIST = \
	$(CSFILES) \
	$(AVAHI_CSFILES) \
	$(man_MANS) \
	AssemblyInfo.cs.in \
	Defines.cs.in \
	$(ASSEMBLY).in \
	$(ASSEMBLY).resources

CLEANFILES = \
	$(ASSEMBLY).exe \
	$(ASSEMBLY).exe.mdb

DISTCLEANFILES = \
	AssemblyInfo.cs \
	Defines.cs \
	$(ASSEMBLY)
