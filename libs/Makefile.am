if USE_SYSTEM_KEYRING
KEYRING_DLL =
else
KEYRING_DLL = ../libs/gnome-keyring-sharp.dll
endif

LAT_DLLS = \
	Novell.Directory.Ldap.dll \
	$(KEYRING_DLL)

LATlibdir = $(prefix)/lib/lat
LATlib_DATA = $(LAT_DLLS)

EXTRA_DIST = \
	$(LAT_DLLS)
