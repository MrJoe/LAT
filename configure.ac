AC_INIT(lat, 0.6, lorenb@mmgsecurity.com)
AC_CANONICAL_TARGET
AM_INIT_AUTOMAKE(AC_PACKAGE_NAME, AC_PACKAGE_VERSION)
AM_MAINTAINER_MODE
AM_CONFIG_HEADER(config.h)

dnl Checks for programs
AC_PROG_INSTALL
AM_PROG_LIBTOOL
AC_PROG_CXX
AC_PROG_INTLTOOL([0.21])

AC_PATH_PROG(PKG_CONFIG, pkg-config, no)
if test "x$PKG_CONFIG" = "xno"; then
	AC_MSG_ERROR([You need to install pkg-config])
fi

AC_PATH_PROG(MONO, mono)
AC_PATH_PROG(MCS, mcs)

AC_MSG_CHECKING([for mono.pc])
if test -z `$PKG_CONFIG --variable=prefix mono`; then
  AC_MSG_ERROR([missing the mono.pc file, usually found in the mono-devel package])
else
  AC_MSG_RESULT([found])
fi

dnl Version dependencies
MONO_REQUIRED=1.1
GTKSHARP_REQUIRED=1.9
KEYRING_REQUIRED=0.0.1

dnl pkgconfig checks
PKG_CHECK_MODULES(MONO,
		  mono >= $MONO_REQUIRED)

PKG_CHECK_MODULES(GTKSHARP,
		  gtk-sharp-2.0 >= $GTKSHARP_REQUIRED \
		  gnome-sharp-2.0 >= $GTKSHARP_REQUIRED \
		  glade-sharp-2.0 >= $GTKSHARP_REQUIRED)

AC_SUBST(GTKSHARP_LIBS)

dnl Check for gnome-keyring-sharp
AC_ARG_ENABLE(system-gnome-keyring,
  AC_HELP_STRING([--enable-system-gnome-keyring],[Use the gnome-keyring-sharp.dll from a system package]),
[case "${enableval}" in
  yes) 
   USE_SYSTEM_KEYRING=yes 
   PKG_CHECK_MODULES(KEYRING, gnome-keyring-sharp >= $KEYRING_REQUIRED)
   AC_SUBST(KEYRING_LIBS)
   ;;
  no)  USE_SYSTEM_KEYRING=no ;;
  *) AC_MSG_ERROR(bad value ${enableval} for --enable-system-gnome-keyring) ;;
esac],
[USE_SYSTEM_KEYRING=no]) dnl Default value

dnl Check to see if we use system or LAT pre-build Novell.Directory.Ldap.dll
AC_ARG_ENABLE(system-ldap,
  AC_HELP_STRING([--enable-system-ldap],[Use the Novell.Directory.Ldap.dll from a system package]),
[case "${enableval}" in
  yes) USE_SYSTEM_LDAP=yes ;;
  no)  USE_SYSTEM_LDAP=no ;;
  *) AC_MSG_ERROR(bad value ${enableval} for --enable-system-ldap) ;;
esac],
[USE_SYSTEM_LDAP=no]) dnl Default value

AM_CONDITIONAL(USE_SYSTEM_KEYRING, test "x$USE_SYSTEM_KEYRING" == "xyes")
AM_CONDITIONAL(USE_SYSTEM_LDAP, test "x$USE_SYSTEM_LDAP" == "xyes")

dnl Check for Mono
AC_PATH_PROG(MCS, mcs)
AC_PATH_PROG(MONO, mono)

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
debian/Makefile
desktop/Makefile
lat/AssemblyInfo.cs
lat/Defines.cs
lat/Makefile
lat/lat
libs/Makefile
po/Makefile.in
help/Makefile
help/C/Makefile
])
