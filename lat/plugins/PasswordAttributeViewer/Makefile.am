CSC = $(MCS) -codepage:utf8 -target:library $(MCS_FLAGS)

ASSEMBLY = PasswordAttributeViewer

CSFILES = \
	PasswordAttributeViewer.cs

SOURCES_BUILD = $(addprefix $(srcdir)/, $(CSFILES))

#if ENABLE_NETWORKMANAGER
NM_REFERENCES = \
        $(top_builddir)/network-manager/network-manager.dll
#endif

REFERENCES = \
	$(NM_REFERENCES) \
	$(top_builddir)/lat/lat.exe

REFERENCES_BUILD = $(addprefix -r:, $(REFERENCES))

$(ASSEMBLY).dll: $(CSFILES)
	$(CSC) -out:$@ $(CSFILES) $(REFERENCES_BUILD) $(GTKSHARP_LIBS)

all: $(ASSEMBLY).dll

ASSEMBLYlibdir = $(pkglibdir)/plugins
ASSEMBLYlib_DATA = $(ASSEMBLY).dll

EXTRA_DIST = \
	$(CSFILES)

CLEANFILES = \
	$(ASSEMBLY).dll \
	$(ASSEMBLY).dll.mdb
