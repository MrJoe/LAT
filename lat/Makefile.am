CSC = $(MCS) -codepage:utf8 $(MCS_FLAGS)

ASSEMBLY = lat

CSFILES = \
	adUserViewDialog.cs \
	AboutDialog.cs \
	ActiveDirectoryComputerViewPlugin.cs \
	ActiveDirectoryContactsViewPlugin.cs \
	ActiveDirectoryGroupViewPlugin.cs \
	ActiveDirectoryUserViewPlugin.cs \
	AssemblyInfo.cs \
	AddAttributeDialog.cs \
	AddObjectClassDialog.cs \
	CertificateDialog.cs \
	ConnectDialog.cs \
	CreateEntryDialog.cs \
	Defines.cs \
	EditAdComputerViewDialog.cs \
	EditContactsViewDialog.cs \
	EditUserViewDialog.cs \
	GroupsViewDialog.cs \
	HostsViewDialog.cs \
	LDIF.cs \
	LdapSearch.cs \
	LdapServer.cs \
	LdapTreeView.cs \
	Logger.cs \
	LoginDialog.cs \
	Main.cs \
	MassEditDialog.cs \
	NewAdComputerViewDialog.cs \
	NewAdGroupViewDialog.cs \
	NewAdUserViewDialog.cs \
	NewContactsViewDialog.cs \
	NewEntryDialog.cs \
	NewUserViewDialog.cs \
	PasswordDialog.cs \
	PosixComputerViewPlugin.cs \
	PosixContactsViewPlugin.cs \
	PosixGroupViewPlugin.cs \
	PosixUserViewPlugin.cs \
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
	Window.cs

SOURCES_BUILD = $(addprefix $(srcdir)/, $(CSFILES))

REFERENCES = \
	../gnome-keyring-glue/gnome-keyring-glue.dll \
	Mono.Posix \
	Mono.Security \
	Novell.Directory.Ldap

REFERENCES_BUILD = $(addprefix -r:, $(REFERENCES))

$(ASSEMBLY).exe: $(CSFILES)
	$(CSC) -out:$@ $(CSFILES) $(REFERENCES_BUILD) @lat.resources $(GTKSHARP_LIBS)

all: $(ASSEMBLY).exe

ASSEMBLYlibdir = $(prefix)/lib/$(ASSEMBLY)
ASSEMBLYlib_DATA = $(ASSEMBLY).exe

man_MANS = \
	$(ASSEMBLY).1

bin_SCRIPTS = $(ASSEMBLY)

EXTRA_DIST = \
	$(CSFILES) \
	$(man_MANS) \
	AssemblyInfo.cs.in \
	Defines.cs.in \
	$(ASSEMBLY).in \
	$(ASSEMBLY).sources \
	$(ASSEMBLY).resources

CLEANFILES = \
	$(ASSEMBLY).exe \
	$(ASSEMBLY).exe.mdb

DISTCLEANFILES = \
	AssemblyInfo.cs \
	Defines.cs \
	$(ASSEMBLY)
