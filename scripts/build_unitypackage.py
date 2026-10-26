"""Package the checked-in Unity assets without running the Unity Editor.

Usage: python scripts/build_unitypackage.py [--output path/to/file.unitypackage]
Only the Python standard library is required. This does not compile C# code.
"""

import argparse
import gzip
import hashlib
import io
import os
from pathlib import Path
import re
import tarfile
import tempfile


ASSET_ROOT = Path('Assets/SteamCapsuleCapture')
# Keep the README GUID used by earlier distributions, even though the source
# README now lives at the repository root rather than inside Assets.
README_META = b'''fileFormatVersion: 2
guid: 77ef6bcc8faef034ea2bb57e5be229ae
TextScriptImporter:
  externalObjects: {}
  userData:
  assetBundleName:
  assetBundleVariant:
'''


def collect_entries(root):
    """Collect archive members, rejecting missing metadata and duplicate GUIDs."""
    asset_root = root / ASSET_ROOT
    if not asset_root.is_dir():
        raise ValueError(f'Missing asset directory: {ASSET_ROOT}')

    entries = {}
    guids = {}

    def add_asset(path, metadata, content):
        matches = re.findall(rb'^guid:[ \t]*([0-9a-fA-F]{32})[ \t]*\r?$',
                             metadata, re.MULTILINE)
        if len(matches) != 1:
            raise ValueError(f'Missing or invalid GUID: {path}.meta')
        guid = matches[0].decode('ascii').lower()
        if guid in guids:
            raise ValueError(f'Duplicate GUID: {path} and {guids[guid]}')
        guids[guid] = path
        if content is None and not re.search(rb'^folderAsset: yes\r?$', metadata, re.MULTILINE):
            raise ValueError(f'Missing folderAsset flag: {path}.meta')
        entries[f'{guid}/pathname'] = path.encode('utf-8')
        entries[f'{guid}/asset.meta'] = metadata
        if content is not None:
            entries[f'{guid}/asset'] = content

    for path in [asset_root] + sorted(asset_root.rglob('*')):
        relative = path.relative_to(root).as_posix()
        if path.is_symlink():
            raise ValueError(f'Symbolic links are not supported: {relative}')
        if path.suffix == '.meta':
            if not path.with_suffix('').exists():
                raise ValueError(f'Orphan metadata: {relative}')
            continue
        meta = Path(str(path) + '.meta')
        if not meta.is_file() or meta.is_symlink():
            raise ValueError(f'Missing or unsupported metadata: {relative}.meta')
        add_asset(relative, meta.read_bytes(), path.read_bytes() if path.is_file() else None)

    # Include the current usage guide without changing the source asset tree.
    readme_path = (ASSET_ROOT / 'README.md').as_posix()
    if readme_path not in guids.values():
        add_asset(readme_path, README_META, (root / 'README.md').read_bytes())
    return entries


def verify_package(payload, expected):
    """Read the complete archive back and compare every file with its source."""
    with tarfile.open(fileobj=io.BytesIO(payload), mode='r:gz') as archive:
        files = [member for member in archive.getmembers() if member.isfile()]
        actual = {member.name: archive.extractfile(member).read() for member in files}
        if len(files) != len(expected) or actual != expected:
            raise ValueError('Package verification failed: archive contents differ from source')


def build_package(root, output):
    entries = collect_entries(root)
    buffer = io.BytesIO()
    # Stable member ordering and timestamps make unchanged sources reproducible.
    with gzip.GzipFile(fileobj=buffer, mode='wb', filename='', mtime=0) as compressed:
        with tarfile.open(fileobj=compressed, mode='w', format=tarfile.USTAR_FORMAT) as archive:
            for guid in sorted({name.split('/')[0] for name in entries}):
                directory = tarfile.TarInfo(guid + '/')
                directory.type = tarfile.DIRTYPE
                directory.mode = 0o755
                archive.addfile(directory)
                for name in sorted(key for key in entries if key.startswith(guid + '/')):
                    data = entries[name]
                    member = tarfile.TarInfo(name)
                    member.size = len(data)
                    member.mode = 0o644
                    archive.addfile(member, io.BytesIO(data))

    payload = buffer.getvalue()
    verify_package(payload, entries)
    output.parent.mkdir(parents=True, exist_ok=True)
    temporary = None
    try:
        with tempfile.NamedTemporaryFile(dir=output.parent, suffix='.tmp', delete=False) as stream:
            temporary = Path(stream.name)
            stream.write(payload)
        verify_package(temporary.read_bytes(), entries)
        os.replace(temporary, output)
    finally:
        if temporary is not None:
            temporary.unlink(missing_ok=True)

    asset_count = sum(name.endswith('/asset') for name in entries)
    print(f'Created {output.name}: {asset_count} files, {len(payload):,} bytes')
    print('Verified source contents, metadata, import paths, and GUID uniqueness.')
    print(f'SHA256: {hashlib.sha256(payload).hexdigest()}')


def main():
    root = Path(__file__).resolve().parents[1]
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path,
                        default=root / 'dist/SteamCapsuleCapture.unitypackage')
    args = parser.parse_args()
    output = args.output.resolve()
    if output.suffix != '.unitypackage':
        parser.error('--output must end with .unitypackage')
    if output.is_relative_to((root / 'Assets').resolve()):
        parser.error('--output must be outside Assets')
    try:
        build_package(root, output)
    except (OSError, ValueError, tarfile.TarError) as error:
        parser.exit(1, f'Package build failed: {error}\n')


if __name__ == '__main__':
    main()
