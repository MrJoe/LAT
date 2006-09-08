AC_INIT(lat, 1.1.6.1, http://dev.mmgsecurity.com/bugs/)
AC_CONFIG_MACRO_DIR(m4)
AM_INIT_AUTOMAKE(AC_PACKAGE_NAME, AC_PACKAGE_VERSION)
AM_MAINTAINER_MODE

dnl Checks for programs
AC_PROG_INSTALL
AC_PROG_INTLTOOL([0.25])
AC_ISC_POSIX

AC_PATH_PROG(PKG_CONFIG, pkg-config, no)
if test "x$PKG_CONFIG" = "xno"; then
	AC_MSG_ERROR([You need to install pkg-config])
fi

AC_PATH_PROG(MONO, mono)
AC_PATH_PROG(MCS, gmcs, no)
if test "x$MCS" = "xno"; then
	AC_MSG_ERROR([You need to install gmcs])
fi

dnl Version dependencies
MONO_REQUIRED=1.1.13
GTKSHARP_REQUIRED=2.8

dnl pkgconfig checks
PKG_CHECK_MODULES(MONO,
		  mono >= $MONO_REQUIRED)

PKG_CHECK_MODULES(GTKSHARP,
		  gtk-sharp-2.0 >= $GTKSHARP_REQUIRED \
		  gnome-sharp-2.0 >= $GTKSHARP_REQUIRED \
		  gconf-sharp-2.0 >= $GTKSHARP_REQUIRED \
		  glade-sharp-2.0 >= $GTKSHARP_REQUIRED)

AC_SUBST(GTKSHARP_LIBS)

dnl gnome-keyring is needed, but only at runtime. Oh well, good to check for it nonetheless
PKG_CHECK_MODULES(GNOME_KEYRING, gnome-keyring-1)

PKG_CHECK_MODULES(NETWORKMANAGER, dbus-sharp, ENABLE_NETWORKMANAGER="yes", ENABLE_NETWORKMANAGER="no")
AC_ARG_ENABLE(networkmanager, 
	      [  --enable-networkmanager[[=no/yes]]    compile with NetworkManager support [[default: yes]]],
	      ENABLE_AVAHI="$enableval")
AM_CONDITIONAL(ENABLE_NETWORKMANAGER, test "$ENABLE_NETWORKMANAGER" = "yes")
AC_SUBST(NETWORKMANAGER_LIBS)

PKG_CHECK_MODULES(AVAHI, avahi-sharp, ENABLE_AVAHI="no", ENABLE_AVAHI="yes")
AC_ARG_ENABLE(avahi, 
	      [  --enable-avahi[[=no/yes]]    compile with avahi support [[default: no]]],
	      ENABLE_AVAHI="$enableval")
AM_CONDITIONAL(ENABLE_AVAHI, test "$ENABLE_AVAHI" = "yes")
AC_SUBST(AVAHI_LIBS)

MCS_FLAGS=
MONO_FLAGS=
if test $USE_MAINTAINER_MODE = yes; then
	MCS_FLAGS="-debug"
	MONO_FLAGS="--debug"
fi
AC_SUBST(MCS_FLAGS)
AC_SUBST(MONO_FLAGS)

dnl Intl
ALL_LINGUAS="fr_FR pt_BR"
GETTEXT_PACKAGE=lat
AC_SUBST(GETTEXT_PACKAGE)
AC_DEFINE_UNQUOTED(GETTEXT_PACKAGE,"$GETTEXT_PACKAGE", [Gettext package])
AM_GLIB_GNU_GETTEXT

AC_OUTPUT([
Makefile
lat.spec
desktop/Makefile
desktop/lat-plugins.pc
gnome-keyring-glue/Makefile
help/Makefile
help/C/Makefile
lat/AssemblyInfo.cs
lat/Defines.cs
lat/Makefile
lat/lat
lat/plugins/Makefile
lat/plugins/PosixCoreViews/Makefile
lat/plugins/ActiveDirectoryCoreViews/Makefile
lat/plugins/JpegAttributeViewer/Makefile
lat/plugins/PasswordAttributeViewer/Makefile
network-manager/Makefile
po/Makefile.in
resources/Makefile
])

echo "
$PACKAGE-$VERSION

    Install Prefix:    ${prefix}
    Mono C# Compiler:  ${MCS}
    Mono Runtime:      ${MONO}

    Features:
      AVAHI support:           		${ENABLE_AVAHI}
      NetworkManager support:  		${ENABLE_NETWORKMANAGER}
"
