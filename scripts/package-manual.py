from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
import platform
import re
import stat
import tempfile
import zipfile
import zlib


ROOT = Path(__file__).resolve().parents[1]
LOADER_755 = ROOT / "artifacts/verification/downloader-inputs/BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.755+3fab71a.zip"
LOADER_788 = ROOT / "artifacts/verification/downloader-inputs/BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.788+5b766a3.zip"
PLUGIN = ROOT / "downloads/hau-v1.0/HAU-1.0-Plugin.zip"
LOADER_755_SHA = "3616D6A67F5F595973EC4AA7BD7EDAF7F799D5BB9926F7146A6DCC7B4ABF478F"
LOADER_788_SHA = "F4CC496BD098A0DF4164B81E3737297707F13A47C2478DBA2F60EEFAB784817A"
DLL_PATH = "BepInEx/plugins/HAU.dll"
OUTPUT_NAME = "HAU-1.0-MicrosoftStore-EpicGames-XboxApp.zip"
STEAM_OUTPUT_NAME = "HAU-1.0-Steam.zip"
LEGAL = ROOT / "installer/ManualLegal"
TIMESTAMP = (2026, 10, 10, 0, 0, 0)
MAX_FILE = 128 * 1024 * 1024
MAX_TOTAL = 512 * 1024 * 1024

INSTALL = """Hacker Among Us 1.0 by ProXgram
Windows PC: Microsoft Store / Epic Games / Xbox App
Among Us 19.0.0 / 2026.9.29

1. Close Among Us.
2. Extract this ZIP, then copy everything inside it into the game folder containing Among Us.exe. Copy BepInEx, dotnet and the loose files directly beside Among Us.exe, not the outer ZIP folder.
3. Open Among Us from your usual launcher. Wait for the first launch to finish preparing the mod.
4. Use your menu key (Delete by default) to open HAU.

Microsoft Store / Xbox App: Xbox App > Among Us > Manage > Files > Browse > Content.
Epic Games: Library > Among Us > Manage > Installation > folder icon.

Updating another menu? Close the game and move every old MalumMenu.dll, HaddadMenu.dll or MalumMenuEnhanced.dll outside BepInEx/plugins, including copies in subfolders. Remove duplicate HAU.dll copies too; keep only BepInEx/plugins/HAU.dll from this ZIP. Your old configuration and saved profile are imported on the first HAU launch and their originals stay intact. Existing HAU settings are kept.

This package is for Windows PC, not Xbox consoles.

Source and credits: https://github.com/ProXgram/HackerAmongUs
"""

STEAM_INSTALL = """Hacker Among Us 1.0 by ProXgram
Windows PC: Steam (64-bit)
Among Us 19.0.0 / 2026.9.29

1. Close Among Us.
2. In Steam, open Library > Among Us > Properties > Installed Files > Browse.
3. Extract this ZIP, then copy everything inside it into the game folder containing Among Us.exe. Copy BepInEx, dotnet and the loose files directly beside Among Us.exe, not the outer ZIP folder.
4. Open Among Us from Steam. Wait for the first launch to finish preparing the mod.
5. Use your menu key (Delete by default) to open HAU.

Updating another menu? Close the game and move every old MalumMenu.dll, HaddadMenu.dll or MalumMenuEnhanced.dll outside BepInEx/plugins, including copies in subfolders. Remove duplicate HAU.dll copies too; keep only BepInEx/plugins/HAU.dll from this ZIP. Your old configuration and saved profile are imported on the first HAU launch and their originals stay intact. Existing HAU settings are kept.

This package requires the 64-bit Steam build of Among Us 19.0.0 / 2026.9.29. Older 32-bit Steam versions need a matching loader and mod version.

Source and credits: https://github.com/ProXgram/HackerAmongUs
"""

AUTOMATIC_UPDATES = """Automatic updates on Steam / Microsoft Store / Xbox App: HAU checks at startup and while the game stays open. Settings also has a Check for updates button. Downloads show progress and retry temporary connection failures. A verified update installs after you close Among Us and keeps your settings. A new game version needs a compatible mod release first. Epic Games uses the latest manual ZIP.

"""


def digest(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest().upper()


def require(condition: bool, message: str) -> None:
    if not condition:
        raise SystemExit(message)


def numeric_version(value: str) -> str:
    parts = value.split(".")
    if not (2 <= len(parts) <= 4 and len(value) <= 43 and all(
        re.fullmatch(r"0|[1-9][0-9]*", part) and int(part) <= 2147483647 for part in parts
    )):
        raise argparse.ArgumentTypeError("Version must contain two to four numeric parts.")
    return value


def sha256_argument(value: str) -> str:
    if not re.fullmatch(r"[0-9a-fA-F]{64}", value):
        raise argparse.ArgumentTypeError("Supply a 64-digit hexadecimal SHA256.")
    return value.upper()


def safe_name(name: str) -> str:
    require(name and "\\" not in name and not name.startswith("/"), "Unsafe package path.")
    normalized = name.rstrip("/")
    require(normalized and len(normalized) <= 240, "Invalid package path length.")
    for part in normalized.split("/"):
        require(part not in ("", ".", "..") and not part.endswith((".", " ")) and
                not any(ord(c) < 32 or c in '<>:"|?*' for c in part), "Unsafe package path.")
        stem = part.split(".")[0].upper()
        require(stem not in ("CON", "PRN", "AUX", "NUL") and
                not re.fullmatch(r"(?:COM|LPT)[1-9¹²³]", stem), "Reserved package file name.")
    return normalized


def pinned_zip(path: Path, expected_hash: str) -> tuple[dict[str, bytes], set[str]]:
    require(path.is_file() and not path.is_symlink(), "A verified release input is missing.")
    require(digest(path.read_bytes()) == expected_hash, "A release input does not match its pinned SHA256.")
    files: dict[str, bytes] = {}
    directories: set[str] = set()
    names: dict[str, bool] = {}
    total = 0
    with zipfile.ZipFile(path) as archive:
        require(len(archive.infolist()) <= 4096, "Too many archive entries.")
        for entry in archive.infolist():
            name = safe_name(entry.filename)
            is_directory = entry.is_dir()
            require(name.lower() not in names, "Duplicate archive destination.")
            names[name.lower()] = is_directory
            mode = entry.external_attr >> 16
            require(not stat.S_ISLNK(mode) and not entry.external_attr & 0x400,
                    "Linked archive entries are not supported.")
            total += entry.file_size
            require(0 <= entry.file_size <= MAX_FILE and total <= MAX_TOTAL, "Archive size exceeds safe limits.")
            if is_directory:
                require(entry.file_size == 0, "A directory contains unexpected data.")
                directories.add(name)
            else:
                data = archive.read(entry)
                require(len(data) == entry.file_size, "An archive file is incomplete.")
                files[name] = data
        require(archive.testzip() is None, "Archive CRC verification failed.")
    for name in names:
        parts = name.split("/")
        for i in range(1, len(parts)):
            ancestor = "/".join(parts[:i])
            require(names.get(ancestor, True), "An archive file blocks a child directory.")
    return files, directories


def legal_assets(draft: bool, loader: dict[str, bytes], legal_root: Path, loader_sha: str) -> tuple[dict[str, bytes], bool]:
    manifest_path = legal_root / "inventory.json"
    if not manifest_path.is_file():
        require(draft, "Required redistribution inventory is not ready. Use --draft for an unfinished preview.")
        return {}, False
    require(not manifest_path.is_symlink(), "The redistribution inventory must be a normal file.")
    manifest = json.loads(manifest_path.read_text(encoding="utf-8-sig"))
    complete = manifest.get("complete") is True
    require(draft or complete, "Redistribution inventory is unfinished. Use --draft for a preview.")
    if complete:
        require(manifest.get("loader_archive", {}).get("sha256") == loader_sha,
                "The redistribution inventory does not describe the selected loader archive.")
        loader_inventory = manifest.get("loader_files")
        require(isinstance(loader_inventory, list) and len(loader_inventory) == len(loader),
                "Redistribution inventory does not cover every loader file.")
        indexed = {safe_name(entry["path"]): entry for entry in loader_inventory}
        require(set(indexed) == set(loader), "Redistribution inventory has missing or duplicate loader paths.")
        for name, data in loader.items():
            require(indexed[name]["sha256"].upper() == digest(data),
                    "A redistribution inventory hash differs from the pinned loader.")
        require(manifest.get("components"), "Redistribution component/source inventory is missing.")
    entries = manifest.get("files")
    require(isinstance(entries, list) and entries, "The redistribution inventory is empty.")
    assets = {"ThirdParty/inventory.json": manifest_path.read_bytes()}
    listed = set()
    for entry in entries:
        relative = safe_name(entry["path"])
        require(relative.lower() not in listed and relative != "inventory.json", "Duplicate redistribution asset.")
        listed.add(relative.lower())
        path = legal_root.joinpath(*relative.split("/"))
        require(path.is_file() and not path.is_symlink() and
                legal_root.resolve() in path.resolve().parents, "A redistribution asset is missing or linked.")
        data = path.read_bytes()
        require(digest(data) == entry["sha256"].upper(), "A redistribution asset differs from its audited hash.")
        require(len(data) <= MAX_FILE, "A redistribution asset is too large.")
        assets["ThirdParty/" + relative] = data
    actual = {p.relative_to(legal_root).as_posix().lower() for p in legal_root.rglob("*") if p.is_file()}
    require(actual == listed | {"inventory.json"}, "Redistribution folder contains unaudited files.")
    return assets, complete


def check_release_text(name: str, data: bytes) -> None:

    markers = [bytes.fromhex(v) for v in ("63686174677074", "636f646578", "6f70656e6169")]
    lower = data.lower()
    for marker in markers:
        require(marker not in lower and marker.decode().encode("utf-16le") not in lower,
                "Release content contains an unwanted build-environment reference: " + name)
    path_markers = [bytes.fromhex(v) for v in ("633a5c75736572735c", "633a2f75736572732f")]
    for marker in path_markers:
        require(marker not in lower and marker.decode().encode("utf-16le") not in lower,
                "Release content contains an absolute workspace path: " + name)


def main() -> None:
    parser = argparse.ArgumentParser(description='Build and verify the complete manual Windows PC package from pinned inputs.')
    parser.add_argument("--draft", action="store_true", help="Build an explicitly unfinished preview.")
    parser.add_argument("--platform", choices=("windows", "steam"), default="windows",
                        help="Choose the existing Windows stores package or the current 64-bit Steam package.")
    parser.add_argument("--version", type=numeric_version, default="1.0.3", help="Finished HAU release version.")
    parser.add_argument("--loader-build", choices=("755", "788"), help="Audited loader build; 1.0 uses 755, newer HAU uses 788.")
    parser.add_argument("--replace", action="store_true", help="Replace an unpublished local package after reviewing a rebuild. Published 1.0 is immutable.")
    parser.add_argument("--plugin-sha", type=sha256_argument, required=True, help="Verified HAU plugin ZIP SHA256.")
    parser.add_argument("--dll-sha", type=sha256_argument, required=True, help="Verified HAU DLL SHA256.")
    args = parser.parse_args()
    steam = args.platform == "steam"
    plugin_path = ROOT / f"downloads/hau-v{args.version}/HAU-{args.version}-Plugin.zip"
    plugin_sha, dll_sha = args.plugin_sha, args.dll_sha
    loader_build = args.loader_build or ("755" if args.version == "1.0" else "788")
    loader_path = LOADER_755 if loader_build == "755" else LOADER_788
    loader_sha = LOADER_755_SHA if loader_build == "755" else LOADER_788_SHA
    legal_root = ROOT / ("installer/ManualLegal" if loader_build == "755" else "installer/ManualLegal788")
    output_name = (STEAM_OUTPUT_NAME if steam else OUTPUT_NAME).replace("-1.0-", f"-{args.version}-")
    loader, directories = pinned_zip(loader_path, loader_sha)
    plugin, _ = pinned_zip(plugin_path, plugin_sha)
    require(len(loader) == 228, "The loader file count differs from the verified release.")
    required_loader = {"winhttp.dll", "doorstop_config.ini", ".doorstop_version",
                       "BepInEx/core/BepInEx.Unity.IL2CPP.dll", "dotnet/coreclr.dll"}
    require(required_loader <= loader.keys(), "The verified loader layout is incomplete.")
    expected_plugin = {DLL_PATH, "README.md", "FORK.md", "CREDITS.md", "FEATURES.md", "LICENSE"}
    require(set(plugin) == expected_plugin and digest(plugin[DLL_PATH]) == dll_sha,
            "The plugin archive differs from its verified release layout.")
    require(DLL_PATH not in loader, "The loader already contains a menu DLL.")
    files = dict(loader)
    files[DLL_PATH] = plugin[DLL_PATH]
    for name in ("LICENSE", "CREDITS.md"):
        path = ROOT / name
        require(path.is_file() and not path.is_symlink(), "A root license or credits file is missing.")
        files[name] = path.read_bytes()
    install = STEAM_INSTALL if steam else INSTALL
    install = install.replace("Hacker Among Us 1.0 by ProXgram", f"Hacker Among Us {args.version} by ProXgram")
    install = install.replace("Source and credits:", AUTOMATIC_UPDATES + "Source and credits:")
    files["INSTALL.txt"] = install.replace("\n", "\r\n").encode("utf-8")
    assets, legal_complete = legal_assets(args.draft, loader, legal_root, loader_sha)
    require(not set(files) & set(assets), "A notice asset would overwrite a loader file.")
    files.update(assets)
    if args.draft:
        files["DRAFT.txt"] = b"UNFINISHED PREVIEW. Redistribution notices are not yet approved. Do not distribute.\r\n"


    for name in (DLL_PATH, "LICENSE", "CREDITS.md", "INSTALL.txt"):
        check_release_text(name, files[name])
    require([n for n in files if n.lower().startswith("bepinex/plugins/") and n.lower().endswith(".dll")] == [DLL_PATH],
            "The package must contain exactly one active menu DLL.")
    require(not any(n.lower().endswith(("among us.exe", "gameassembly.dll", "unityplayer.dll")) or
                    n.lower().startswith("among us_data/") for n in files), "Game files must not be redistributed.")
    names = {safe_name(n).lower() for n in files}
    require(len(names) == len(files), "Case-insensitive package collision.")
    require(sum(len(v) for v in files.values()) <= MAX_TOTAL, "The complete package exceeds its safe size limit.")

    output_directory = ROOT / ("artifacts/verification/manual-package" if args.draft else "downloads/manual")
    output_directory.mkdir(parents=True, exist_ok=True)
    output = output_directory / (output_name.replace(".zip", "-DRAFT.zip") if args.draft else output_name)
    require(not output.exists() or args.draft or args.replace and args.version != "1.0",
            "An existing release cannot be replaced implicitly; use a new version or explicitly --replace an unpublished package.")
    with tempfile.TemporaryDirectory(prefix="MenuManualPackage-") as temporary:
        staging_zip = Path(temporary) / output.name
        with zipfile.ZipFile(staging_zip, "w", zipfile.ZIP_DEFLATED, compresslevel=9) as archive:
            for name in sorted(directories):
                entry = zipfile.ZipInfo(name + "/", TIMESTAMP)
                entry.create_system = 3
                entry.external_attr = (stat.S_IFDIR | 0o755) << 16 | 0x10
                archive.writestr(entry, b"")
            for name, data in sorted(files.items()):
                entry = zipfile.ZipInfo(name, TIMESTAMP)
                entry.create_system = 3
                entry.external_attr = (stat.S_IFREG | 0o644) << 16
                entry.compress_type = zipfile.ZIP_DEFLATED
                archive.writestr(entry, data, compresslevel=9)
        with zipfile.ZipFile(staging_zip) as archive:
            require(archive.testzip() is None, "The output archive failed CRC validation.")
            require({i.filename for i in archive.infolist() if not i.is_dir()} == set(files), "The output layout differs from the plan.")
            for name, data in files.items():
                require(archive.read(name) == data, "A packaged file differs from its verified bytes.")
            extraction = Path(temporary) / "extracted"
            archive.extractall(extraction)
            require((extraction / "Among Us.exe").exists() is False and
                    (extraction / DLL_PATH).is_file(), "The extracted package layout is invalid.")
            for name, data in loader.items():
                require((extraction / name).read_bytes() == data, "An extracted official loader file changed.")
        require(staging_zip.stat().st_size < 100 * 1024 * 1024, "The archive exceeds the repository download limit.")

        output.write_bytes(staging_zip.read_bytes())

    report = {
        "package": output.relative_to(ROOT).as_posix(),
        "draft": args.draft,
        "bytes": output.stat().st_size,
        "sha256": digest(output.read_bytes()),
        "loaderArchiveSha256": loader_sha,
        "loaderBuild": loader_build,
        "pluginArchiveSha256": plugin_sha,
        "pluginDllSha256": dll_sha,
        "officialLoaderFiles": len(loader),
        "allOfficialLoaderBytesPreserved": True,
        "menuDllCount": 1,
        "gameFilesBundled": False,
        "safePaths": True,
        "crcVerified": True,
        "extractedLayoutVerified": True,
        "redistributionInventoryComplete": legal_complete,
        "redistributionLoaderInventoryVerified": legal_complete,
        "thirdPartyAssetCount": len(assets),
        "python": platform.python_version(),
        "zlib": zlib.ZLIB_VERSION,
        "fixedZipTimestamp": list(TIMESTAMP),
        "epicGamesLiveTested": False,
        "modVersion": args.version,
        "automaticUpdatesIncluded": True,
        "legacySettingsMigrationIncluded": True,
    }
    if steam:
        report.update({"platform": "Steam", "architecture": "x64", "steamLiveTested": False})
    verification = ROOT / "artifacts/verification"
    verification.mkdir(parents=True, exist_ok=True)
    report_name = ("hau-manual-package-steam-" if steam else "hau-manual-package-") + args.version
    (verification / (report_name + ("-draft.json" if args.draft else ".json"))).write_text(
        json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main()
