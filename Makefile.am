ACLOCAL_AMFLAGS = -I m4

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

