if USE_SYSTEM_KEYRING
KEYRING_DLL = $(KEYRING_LIBS)
else
KEYRING_DLL = ../libs/gnome-keyring-sharp.dll
endif

if USE_SYSTEM_LDAP
LDAP_DLL = Novell.Directory.Ldap.dll
LDAP_DEFINE = /d:SYSTEM_LDAP
else
LDAP_DLL = ../libs/Novell.Directory.Ldap.dll
LDAP_DEFINE =
endif

CSC = $(MCS) -codepage:utf8 $(MCS_FLAGS) $(LDAP_DEFINE)

LAT_ASSEMBLY = lat.exe

LAT_CSFILES = \
	adUserViewDialog.cs \
	AboutDialog.cs \
	AssemblyInfo.cs \
	AddAttributeDialog.cs \
	AddEntryDialog.cs \
	CertificateDialog.cs \
	CertMgr.cs \
	ConnectDialog.cs \
	Connection.cs \
	ConnectionProfile.cs \
	ContactsView.cs \
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
	LdapTreeView.cs \
	Logger.cs \
	LoginDialog.cs \
	Main.cs \
	NewAdComputerViewDialog.cs \
	NewContactsViewDialog.cs \
	NewUserViewDialog.cs \
	PasswordDialog.cs \
	ProfileDialog.cs \
	ProfileManager.cs \
	RenameEntryDialog.cs \
	SMBPassword.cs \
	SchemaTreeView.cs \
	SearchBuilderDialog.cs \
	SelectContainerDialog.cs \
	UsersView.cs \
	Util.cs \
	View.cs \
	ViewDialog.cs \
	ViewDialogFactory.cs \
	ViewFactory.cs \
	Window.cs

LAT_SOURCES_BUILD = $(addprefix $(srcdir)/, $(LAT_CSFILES))

LAT_REFERENCES = \
	$(LDAP_DLL) \
	$(KEYRING_DLL) \
	Mono.Posix \
	Mono.Security

LAT_REFERENCES_BUILD = $(addprefix -r:, $(LAT_REFERENCES))

RESOURCES = \
	/resource:$(srcdir)/lat.glade,lat.glade

$(LAT_ASSEMBLY): $(LAT_CSFILES)
	$(CSC) -out:$@ $(LAT_SOURCES_BUILD) $(LAT_REFERENCES_BUILD) $(RESOURCES) $(GTKSHARP_LIBS)

all: $(LAT_ASSEMBLY)

LATlibdir = $(prefix)/lib/lat
LATlib_DATA = $(LAT_ASSEMBLY)

man_MANS = \
	lat.1

bin_SCRIPTS = lat

EXTRA_DIST = \
	$(LAT_CSFILES) \
	$(man_MANS) \
	AssemblyInfo.cs.in \
	Defines.cs.in \
	lat.glade \
	lat.in

CLEANFILES = \
	$(LAT_ASSEMBLY) \
	lat.exe.mdb

DISTCLEANFILES = \
	AssemblyInfo.cs \
	Defines.cs \
	lat
