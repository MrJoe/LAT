#!/bin/sh
intltoolize --force --copy || exit 1
autoreconf -v --force --install || exit 1
./configure --enable-maintainer-mode "$@"

