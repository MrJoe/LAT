CSC = $(MCS) $(MCS_FLAGS) -unsafe -target:library

ASSEMBLY = gnome-keyring-glue

CSFILES = \
	Attribute.cs \
	AttributeType.cs \
	Found.cs \
	Global.cs \
	GnomeKeyringSharp.OperationGetIntCallbackNative.cs \
	GnomeKeyringSharp.OperationGetListCallbackNative.cs \
	ItemType.cs \
	NetworkPasswordData.cs \
	OperationGetIntCallback.cs \
	OperationGetListCallback.cs \
	Result.cs

SOURCES_BUILD = $(addprefix $(srcdir)/, $(CSFILES))

$(ASSEMBLY).dll: $(CSFILES)
	$(CSC) -out:$@ $(SOURCES_BUILD) $(GTKSHARP_LIBS)

all: $(ASSEMBLY).dll

ASSEMBLYlibdir = $(prefix)/lib/lat
ASSEMBLYlib_DATA = $(ASSEMBLY).dll

EXTRA_DIST = \
	$(CSFILES) \
	AUTHORS \
	LICENSE.LGPL

CLEANFILES = \
	$(ASSEMBLY).dll \
	$(ASSEMBLY).dll.mdb
