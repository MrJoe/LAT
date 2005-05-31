AC_INIT(lat, 0.5.3, lorenb@mmgsecurity.com)
AC_PREREQ(2.52)
AC_CONFIG_SRCDIR(lat/Main.cs)
AM_INIT_AUTOMAKE(AC_PACKAGE_NAME, AC_PACKAGE_VERSION)
AM_CONFIG_HEADER(config.h)
AM_MAINTAINER_MODE

dnl Checks for programs
AC_ISC_POSIX
AC_PROG_INSTALL
AM_PROG_LIBTOOL
AC_PROG_INTLTOOL([0.21])

dnl Version dependencies
MONO_REQUIRED=1.1
GTKSHARP_REQUIRED=1.0

dnl pkgconfig checks
PKG_CHECK_MODULES(MONO,
		  mono >= $MONO_REQUIRED)

PKG_CHECK_MODULES(GTKSHARP,
		  gtk-sharp >= $GTKSHARP_REQUIRED \
		  gnome-sharp >= $GTKSHARP_REQUIRED \
		  glade-sharp >= $GTKSHARP_REQUIRED)

AC_SUBST(GTKSHARP_LIBS)

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
ALL_LINGUAS=""
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
po/Makefile.in
])

