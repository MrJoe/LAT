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
	PosixUserViewPlugin.cs

SOURCES_BUILD = $(addprefix $(srcdir)/, $(CSFILES))

REFERENCES = \
	Mono.Posix \
	Mono.Security \
	Novell.Directory.Ldap \
	$(top_builddir)/lat/lat.exe

REFERENCES_BUILD = $(addprefix -r:, $(REFERENCES))

RESOURCES = \
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

ASSEMBLYlibdir = $(prefix)/lib/lat/plugins
ASSEMBLYlib_DATA = $(ASSEMBLY).dll

EXTRA_DIST = \
	$(CSFILES)

CLEANFILES = \
	$(ASSEMBLY).dll \
	$(ASSEMBLY).dll.mdb
