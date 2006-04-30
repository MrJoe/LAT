@INTLTOOL_DESKTOP_RULE@

desktop_in_files = lat.desktop.in
desktop_files = $(desktop_in_files:.desktop.in=.desktop)

desktopdir = $(datadir)/applications
desktop_DATA = $(desktop_files)

appicon_DATA = lat.png
appicondir = $(datadir)/pixmaps

applicationsdir = $(datadir)/application-registry
applications_DATA = lat.applications

pkgconfigdir = $(libdir)/pkgconfig
pkgconfig_in_files = lat-plugins.pc.in
pkgconfig_DATA = $(pkgconfig_in_files:.pc.in=.pc)

EXTRA_DIST =			\
	$(desktop_in_files)	\
	$(desktop_DATA)		\
	$(applications_DATA)	\
	$(appicon_DATA)		\
	$(pkgconfig_in_files)

CLEANFILES = 		\
	$(desktop_DATA) \
	$(applications_DATA) \
	$(pkgconfig_DATA)
