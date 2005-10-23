AC_INIT(lat, 0.7.4, lorenb@mmgsecurity.com)
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
GTKSHARP_REQUIRED=2.3

dnl pkgconfig checks
PKG_CHECK_MODULES(MONO,
		  mono >= $MONO_REQUIRED)

PKG_CHECK_MODULES(GTKSHARP,
		  gtk-sharp-2.0 >= $GTKSHARP_REQUIRED \
		  gnome-sharp-2.0 >= $GTKSHARP_REQUIRED \
		  glade-sharp-2.0 >= $GTKSHARP_REQUIRED)

AC_SUBST(GTKSHARP_LIBS)

dnl gnome-keyring is needed, but only at runtime. Oh well, good to check for it nonetheless
PKG_CHECK_MODULES(GNOME_KEYRING, gnome-keyring-1)

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
gnome-keyring-glue/Makefile
lat/AssemblyInfo.cs
lat/Defines.cs
lat/Makefile
lat/lat
po/Makefile.in
help/Makefile
help/C/Makefile
])
