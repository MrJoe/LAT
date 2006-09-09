CSC = $(MCS) -codepage:utf8 -target:library $(MCS_FLAGS)

ASSEMBLY = JpegAttributeViewer

CSFILES = \
	JpegAttributeViewer.cs

SOURCES_BUILD = $(addprefix $(srcdir)/, $(CSFILES))

#if ENABLE_NETWORKMANAGER
NM_REFERENCES = \
	$(top_builddir)/network-manager/network-manager.dll
#endif

REFERENCES = \
	$(NM_REFERENCES) \
	$(top_builddir)/lat/lat.exe

REFERENCES_BUILD = $(addprefix -r:, $(REFERENCES))

RESOURCES = \
	dialog.glade

RESOURCES_BUILD = $(addprefix /resource:, $(RESOURCES))

$(ASSEMBLY).dll: $(CSFILES)
	$(CSC) -out:$@ $(CSFILES) $(REFERENCES_BUILD) $(RESOURCES_BUILD) $(GTKSHARP_LIBS)

all: $(ASSEMBLY).dll

ASSEMBLYlibdir = $(pkglibdir)/plugins
ASSEMBLYlib_DATA = $(ASSEMBLY).dll

EXTRA_DIST = \
	$(CSFILES) \
	dialog.glade

CLEANFILES = \
	$(ASSEMBLY).dll \
	$(ASSEMBLY).dll.mdb
