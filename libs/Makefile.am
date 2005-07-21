if USE_SYSTEM_KEYRING
KEYRING_DLL =
else
KEYRING_DLL = gnome-keyring-sharp.dll
endif

if USE_SYSTEM_LDAP
LDAP_DLL =
else
LDAP_DLL = Novell.Directory.Ldap.dll
endif

LAT_DLLS = \
	$(LDAP_DLL) \
	$(KEYRING_DLL)

LATlibdir = $(prefix)/lib/lat
LATlib_DATA = $(LAT_DLLS)

EXTRA_DIST = \
	$(LAT_DLLS)
