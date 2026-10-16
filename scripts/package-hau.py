from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path, PurePosixPath
import re
import shutil
import subprocess
import zipfile
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
DOCUMENTS = ("README.md", "FORK.md", "CREDITS.md", "FEATURES.md", "LICENSE")
PLUGIN_ENTRY = "BepInEx/plugins/HAU.dll"
TIMESTAMP = (2026, 10, 10, 0, 0, 0)


def require(condition: bool, message: str) -> None:
    if not condition:
        raise SystemExit(message)


def numeric_version(value: str) -> str:
    parts = value.split(".")
    if not (2 <= len(parts) <= 4 and all(re.fullmatch(r"0|[1-9][0-9]*", part) and int(part) <= 2147483647 for part in parts)):
        raise argparse.ArgumentTypeError("Supply two to four numeric version parts.")
    return value


def sha256(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest().upper()


def put(archive: zipfile.ZipFile, name: str, data: bytes) -> None:
    entry = zipfile.ZipInfo(name, TIMESTAMP)
    entry.compress_type = zipfile.ZIP_DEFLATED
    entry.external_attr = 0o100644 << 16
    archive.writestr(entry, data)


def main() -> None:
    parser = argparse.ArgumentParser(description='Package a finished HAU build and its matching source without touching historical releases.')
    parser.add_argument("--version", type=numeric_version, default="1.0.3")
    parser.add_argument("--dll", type=Path, help="Finished HAU.dll; defaults to src/bin/Release/net6.0/HAU.dll.")
    parser.add_argument("--replace", action="store_true", help="Replace an existing local HAU package after reviewing a rebuilt release.")
    args = parser.parse_args()
    requested_build = (args.dll or ROOT / "src/bin/Release/net6.0/HAU.dll").expanduser()
    built = requested_build.resolve()
    require(built.is_file() and not requested_build.is_symlink() and ROOT in built.parents and built.name == "HAU.dll",
            "A finished normal HAU.dll build inside this repository is required.")
    project = ET.parse(ROOT / "src/HAU.csproj").getroot()
    require(project.findtext(".//AssemblyName") == "HAU" and project.findtext(".//VersionPrefix") == args.version,
            "The HAU project identity or version does not match this release.")
    branding = (ROOT / "src/ModBranding.cs").read_text(encoding="utf-8-sig")
    require('Name = "Hacker Among Us"' in branding and 'Creator = "ProXgram"' in branding and
            f'Version = "{args.version}"' in branding, "The release must match the HAU branding and numeric version.")
    git_root = subprocess.check_output(["git", "-C", str(ROOT), "rev-parse", "--show-toplevel"], text=True).strip()
    require(Path(git_root).resolve() == ROOT, "Package from this source repository's own root.")
    names = subprocess.check_output(["git", "-C", str(ROOT), "ls-files", "--cached", "--others", "--exclude-standard", "-z"]).decode("utf-8").split("\0")
    source_files = sorted({name for name in names if name and not any(
        part.lower() in ("artifacts", "bin", "obj", ".git", "downloads") for part in PurePosixPath(name).parts
    ) and (ROOT / name).is_file()})
    release = ROOT / f"artifacts/release/hau-v{args.version}"
    downloads = ROOT / f"downloads/hau-v{args.version}"
    plugin_zip = release / f"HAU-{args.version}-Plugin.zip"
    source_zip = release / f"HAU-{args.version}-Source.zip"
    outputs = (plugin_zip, source_zip, downloads / plugin_zip.name, downloads / source_zip.name)
    require(args.replace or not any(path.exists() for path in outputs),
            "This HAU release already has local archives. Review the rebuild, then explicitly use --replace.")
    require(all(not path.is_symlink() for path in outputs), "A release output cannot be linked.")
    release.mkdir(parents=True, exist_ok=True)
    downloads.mkdir(parents=True, exist_ok=True)
    binary = built.read_bytes()
    require(len(binary) > 0 and binary[:2] == b"MZ", "The finished HAU build is not a Windows assembly.")
    require(not any(marker in binary.lower() or marker.decode().encode("utf-16le") in binary.lower()
                    for marker in (b"judge-test", b"guest-test")), "Experimental builds cannot be published as a normal HAU release.")
    with zipfile.ZipFile(plugin_zip, "w", zipfile.ZIP_DEFLATED) as archive:
        put(archive, PLUGIN_ENTRY, binary)
        for name in DOCUMENTS:
            require((ROOT / name).is_file() and not (ROOT / name).is_symlink(), "A release document is missing or linked.")
            put(archive, name, (ROOT / name).read_bytes())
    with zipfile.ZipFile(plugin_zip) as archive:
        require(set(archive.namelist()) == {PLUGIN_ENTRY, *DOCUMENTS} and archive.testzip() is None,
                "The plugin ZIP has an unexpected layout or damaged contents.")
        require(archive.read(PLUGIN_ENTRY) == binary and archive.read("LICENSE") == (ROOT / "LICENSE").read_bytes(),
                "The packaged plugin or license differs from the finished source inputs.")
    with zipfile.ZipFile(source_zip, "w", zipfile.ZIP_DEFLATED) as archive:
        for name in source_files:
            path = ROOT / name
            require(not path.is_symlink() and ROOT in path.resolve().parents, "Corresponding source cannot include linked files.")
            put(archive, name, path.read_bytes())
    with zipfile.ZipFile(source_zip) as archive:
        require(archive.testzip() is None and set(archive.namelist()) == set(source_files), "Source package verification failed.")
        require(all(archive.read(name) == (ROOT / name).read_bytes() for name in source_files),
                "The source changed while being packaged. Rebuild the release after finishing edits.")
        require({"src/HAU.csproj", "src/ModBranding.cs", "CREDITS.md", "LICENSE"} <= set(archive.namelist()),
                "The corresponding source lacks required project or license files.")
    shutil.copy2(plugin_zip, downloads / plugin_zip.name)
    shutil.copy2(source_zip, downloads / source_zip.name)
    shutil.copy2(built, release / "HAU.dll")
    report = {"name": "Hacker Among Us", "creator": "ProXgram", "version": args.version,
              "pluginPackage": (downloads / plugin_zip.name).relative_to(ROOT).as_posix(),
              "pluginArchiveSha256": sha256(plugin_zip.read_bytes()), "dllSha256": sha256(binary),
              "sourcePackage": (downloads / source_zip.name).relative_to(ROOT).as_posix(),
              "sourceEntries": len(source_files), "oneMenuPluginInPackage": True,
              "sourceMatchesRepository": True, "installed": False, "startupVerified": False}
    verification = ROOT / "artifacts/verification"
    verification.mkdir(parents=True, exist_ok=True)
    (verification / f"hau-plugin-{args.version}.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main()
