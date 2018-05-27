#!/usr/bin/env python3
#
# Copyright 2010-2018 Jesse McGrew
#
# This file is part of ZILF.
#
# ZILF is free software: you can redistribute it and/or modify it
# under the terms of the GNU General Public License as published by
# the Free Software Foundation, either version 3 of the License, or
# (at your option) any later version.
#
# ZILF is distributed in the hope that it will be useful, but
# WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
# General Public License for more details.
#
# You should have received a copy of the GNU General Public License
# along with ZILF.  If not, see <http://www.gnu.org/licenses/>.
#

"""Packages the ZILF binaries and other content for release."""

from contextlib import contextmanager
import datetime
import glob
import os
import re
import sys
from types import TracebackType
from typing import Iterable, Generator, Optional, Set, ContextManager, Type
from zipfile import ZipFile, ZIP_LZMA

class Archiver(ContextManager['Archiver']):
    def __init__(self, archive_path: str, src_dir: str) -> None:
        self.zipfile = ZipFile(archive_path, 'w', ZIP_LZMA)
        self.src_dir = src_dir
        self.added_paths = set() # type: Set[str]

    def __enter__(self) -> 'Archiver':
        self.zipfile.__enter__()
        return self

    def __exit__(self,
                 exc_type: Optional[Type[BaseException]],
                 exc_value: Optional[BaseException],
                 traceback: Optional[TracebackType]) -> Optional[bool]:
        self.zipfile.__exit__(exc_type, exc_value, traceback)
        return False

    def _resolve_src(self, src: str) -> str:
        src = src.replace('/', os.path.sep)
        return os.path.join(self.src_dir, src)

    def _resolve_dest(self, dest: str, src: str) -> str:
        if dest.endswith('/'):
            dest += os.path.basename(src)
        return dest

    def _add(self, src: str, dest: str) -> None:
        print(f'{src} -> {dest}')
        if dest not in self.added_paths:
            self.added_paths.add(dest)
            self.zipfile.write(src, dest)

    def add_static(self, dest: str, src: str) -> None:
        src = self._resolve_src(src)
        dest = self._resolve_dest(dest, src)
        self._add(src, dest)

    def add_project_output(self, dest: str, project: str) -> None:
        # TODO: netcoreapp2.0?
        bin_dir = os.path.join(self._resolve_src(project), 'bin', 'Release', 'net471')
        assert os.path.exists(bin_dir), 'missing bin dir: %s' % bin_dir
        for pattern in ('*.exe', '*.dll'):
            for this_src in glob.iglob(os.path.join(bin_dir, pattern), recursive = True):
                this_dest = self._resolve_dest(dest, this_src)
                self._add(this_src, this_dest)

    def add_glob(self, dest: str, pattern: str) -> None:
        pattern = self._resolve_src(pattern)
        for this_src in glob.iglob(pattern, recursive = True):
            this_dest = self._resolve_dest(dest, this_src)
            self._add(this_src, this_dest)


def find_sln_dir() -> str:
    result = os.path.realpath(__file__)
    while not os.path.exists(os.path.join(result, 'Zilf.sln')):
        result = os.path.dirname(result)
        assert result
    return result


def format_zip_name(sln_dir: str) -> str:
    date = datetime.datetime.today().strftime('%y%m%d')

    # read version from constant in Zilf/Program.cs
    csfile = os.path.join(sln_dir, 'Zilf', 'Program.cs')

    with open(csfile, 'r') as f:
        code = f.read()

    m = re.search(r'VERSION\s*=\s*"(?:ZILF\s*)?([0-9.]+)"', code)
    assert m
    version = m.group(1)

    return f'zilf-{version}-{date}.zip'


def make_dist() -> str:
    """Builds the archive and returns its filename."""
    sln_dir = find_sln_dir()
    output_dir = os.path.join(sln_dir, 'dist')

    try:
        os.mkdir(output_dir)
    except FileExistsError:
        pass

    output = os.path.join(output_dir, format_zip_name(sln_dir))

    try:
        os.unlink(output)
    except FileNotFoundError:
        pass

    with Archiver(output, sln_dir) as d:
        d.add_static('/', 'COPYING.txt')
        d.add_static('/', 'distfiles/README.txt')

        d.add_project_output('bin/', 'Zapf')
        d.add_project_output('bin/', 'Zilf')

        d.add_static('doc/', 'Zilf/quickref.txt')
        d.add_static('doc/', 'zapf_manual.html')
        d.add_static('doc/', 'zilf_manual.html')

        d.add_static('library/', 'Library/LICENSE.txt')
        d.add_static('library/', 'Library/ZIL_ZILF_differences.txt')
        d.add_glob('library/', 'Library/*.mud')
        d.add_glob('library/', 'Library/*.zil')

        d.add_glob('sample/advent/', 'Examples/advent/*.zil')

        d.add_glob('sample/cloak/', 'Examples/cloak/*.zil')

        d.add_glob('sample/cloak_plus/', 'Examples/cloak_plus/*.md')
        d.add_glob('sample/cloak_plus/', 'Examples/cloak_plus/*.zil')

        d.add_glob('sample/empty/', 'Examples/empty/*.zil')

    return output


def main() -> int:
    """Builds the archive, prints its filename, and returns an exit status."""
    output = make_dist()
    print(output)
    return 0


if __name__ == '__main__':
    sys.exit(main())
