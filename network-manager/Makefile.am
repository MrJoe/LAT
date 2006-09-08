CSC = $(MCS) $(MCS_FLAGS) -target:library

ASSEMBLY = network-manager

CSFILES = \
	Device.cs \
	Manager.cs \
	Network.cs \
	NetworkDetect.cs

SOURCES_BUILD = $(addprefix $(srcdir)/, $(CSFILES))

REFERENCES = \
	Mono.Posix

REFERENCES_BUILD = $(addprefix -r:, $(REFERENCES))

$(ASSEMBLY).dll: $(CSFILES)
	$(CSC) -out:$@ $(SOURCES_BUILD) $(REFERENCES_BUILD) $(NETWORKMANAGER_LIBS)

all: $(ASSEMBLY).dll

ASSEMBLYlibdir = $(pkglibdir)
ASSEMBLYlib_DATA = $(ASSEMBLY).dll

EXTRA_DIST = \
	$(CSFILES)

CLEANFILES = \
	$(ASSEMBLY).dll \
	$(ASSEMBLY).dll.mdb
