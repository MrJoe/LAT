@INTLTOOL_DESKTOP_RULE@

desktop_in_files = lat.desktop.in
desktop_files = $(desktop_in_files:.desktop.in=.desktop)

desktopdir = $(datadir)/applications
desktop_DATA = $(desktop_files)

applicationsdir = $(datadir)/application-registry
applications_DATA = lat.applications

EXTRA_DIST =			\
	$(desktop_in_files)	\
	$(desktop_DATA)		\
	$(applications_DATA)

CLEANFILES = 		\
	$(desktop_DATA)
