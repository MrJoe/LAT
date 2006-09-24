#if ENABLE_NETWORKMANAGER
NM_DIR = network-manager
#endif

SUBDIRS = gnome-keyring-glue $(NM_DIR) lat po desktop resources help

EXTRA_DIST = \
	README \
	COPYING \
	COPYING-DOCS \
	AUTHORS \
	ChangeLog \
	INSTALL \
	NEWS \
	TODO \
	lat.spec.in \
	lat.spec \
	intltool-extract.in \
	intltool-merge.in \
	intltool-update.in \
	omf.make \
	xmldocs.make

DISTCLEANFILES = \
	intltool-extract \
	intltool-merge \
	intltool-update

distuninstallcheck_listfiles = \
        find -path ./var/scrollkeeper/\* -prune -or -type f -print \
        find -path $(prefix)/var/scrollkeeper/\* -prune -or -type f -print
