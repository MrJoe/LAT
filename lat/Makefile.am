CSC = $(MCS) -codepage:utf8 $(MCS_FLAGS)

ASSEMBLY = lat

CSFILES = \
	adUserViewDialog.cs \
	AboutDialog.cs \
	AssemblyInfo.cs \
	AddAttributeDialog.cs \
	CertificateDialog.cs \
	ConnectDialog.cs \
	ContactsView.cs \
	CreateEntryDialog.cs \
	CustomView.cs \
	CustomViewDialog.cs \
	CustomViewManager.cs \
	Defines.cs \
	DynamicView.cs \
	DynamicViewDialog.cs \
	EditAdComputerViewDialog.cs \
	EditContactsViewDialog.cs \
	EditUserViewDialog.cs \
	GroupsView.cs \
	GroupsViewDialog.cs \
	HostsView.cs \
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
	NewContactsViewDialog.cs \
	NewEntryDialog.cs \
	NewUserViewDialog.cs \
	PasswordDialog.cs \
	ProfileDialog.cs \
	ProfileManager.cs \
	RenameEntryDialog.cs \
	SMBPassword.cs \
	SambaPopulateDialog.cs \
	SchemaTreeView.cs \
	SearchBuilderDialog.cs \
	SearchResultsTreeView.cs \
	SelectContainerDialog.cs \
	Templates.cs \
	TemplatesDialog.cs \
	TemplateEditorDialog.cs \
	TimeDateDialog.cs \
	UsersView.cs \
	Util.cs \
	View.cs \
	ViewDialog.cs \
	ViewDialogFactory.cs \
	ViewFactory.cs \
	ViewsTreeView.cs \
	Window.cs

SOURCES_BUILD = $(addprefix $(srcdir)/, $(CSFILES))

REFERENCES = \
	../gnome-keyring-glue/gnome-keyring-glue.dll \
	Mono.Posix \
	Mono.Security \
	Novell.Directory.Ldap

REFERENCES_BUILD = $(addprefix -r:, $(REFERENCES))

RESOURCES = \
	/resource:$(srcdir)/$(ASSEMBLY).glade,$(ASSEMBLY).glade

$(ASSEMBLY).exe: $(CSFILES)
	$(CSC) -out:$@ $(SOURCES_BUILD) $(REFERENCES_BUILD) $(RESOURCES) $(GTKSHARP_LIBS)

all: $(ASSEMBLY).exe

ASSEMBLYlibdir = $(prefix)/lib/lat
ASSEMBLYlib_DATA = $(ASSEMBLY).exe

man_MANS = \
	$(ASSEMBLY).1

bin_SCRIPTS = $(ASSEMBLY)

EXTRA_DIST = \
	$(CSFILES) \
	$(man_MANS) \
	AssemblyInfo.cs.in \
	Defines.cs.in \
	$(ASSEMBLY).glade \
	$(ASSEMBLY).in

CLEANFILES = \
	$(ASSEMBLY).exe \
	$(ASSEMBLY).exe.mdb

DISTCLEANFILES = \
	AssemblyInfo.cs \
	Defines.cs \
	$(ASSEMBLY)
