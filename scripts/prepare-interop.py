"""Prepare hash-pinned build references for HAU; never reads or changes a game install."""
from __future__ import annotations

import argparse
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import shutil
import stat
import subprocess
import tempfile
import urllib.request
import zipfile

ROOT = Path(__file__).resolve().parents[1]
CACHE = ROOT / "artifacts" / "interop"
OUTPUT = CACHE / "among-us-2026.9.29-interop-1.5.3"
ASSETS = {
    "cli": (
        "Il2CppInterop.CLI.1.5.3.zip",
        "https://github.com/BepInEx/Il2CppInterop/releases/download/v1.5.3/Il2CppInterop.CLI.1.5.3.zip",
        "2600DB9D9F14EED2EA389C2CCAB1E50BF3BE22D11A6F11693AA723EF305A6595",
        53307310,
    ),
    "unity": (
        "Unity-2022.3.44.zip",
        "https://unity.bepinex.dev/libraries/2022.3.44.zip",
        "E9E6C943619867F0AAFB6888BD57EC49E46F833B92EC0E43223346370D69E0BD",
        1896776,
    ),
    "sdk": (
        "amongus.gamelibs.steam.2026.9.29.nupkg",
        "https://nuget.bepinex.dev/v3/package/amongus.gamelibs.steam/2026.9.29/amongus.gamelibs.steam.2026.9.29.nupkg",
        "9DCD24F9E44A628A191DF0168DD1D7152120F96AD3FF06CCB3962BEABAEFDBFC",
        7625577,
    ),
}
SPEC = {"gameVersion": "2026.9.29", "unityVersion": "2022.3.44", "interopVersion": "1.5.3",
        "loaderBuild": 788, "assets": {key: data[2] for key, data in ASSETS.items()},
        "arguments": ["generate", "--input", "sdk/build", "--output", "references", "--unity", "unity", "--no-xref-cache"]}


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest().upper()


def require_asset(path: Path, expected_sha: str, expected_size: int) -> None:
    if path.is_symlink() or not path.is_file() or path.stat().st_size != expected_size or sha256(path) != expected_sha:
        raise ValueError(f"Build input failed its size/SHA-256 check: {path.name}")


def download_asset(key: str) -> Path:
    name, url, expected_sha, expected_size = ASSETS[key]
    path = CACHE / "downloads" / name
    path.parent.mkdir(parents=True, exist_ok=True)
    if path.exists():
        require_asset(path, expected_sha, expected_size)
        return path
    temporary = path.with_suffix(path.suffix + f".{os.getpid()}.part")
    try:
        request = urllib.request.Request(url, headers={"User-Agent": "HAU-build-references/1.0.1"})
        with urllib.request.urlopen(request, timeout=120) as response, temporary.open("xb") as target:
            total = 0
            while block := response.read(1024 * 1024):
                total += len(block)
                if total > expected_size:
                    raise ValueError(f"Build download exceeds its pinned size: {name}")
                target.write(block)
        require_asset(temporary, expected_sha, expected_size)
        temporary.replace(path)
        return path
    finally:
        temporary.unlink(missing_ok=True)


def extract_archive(source: Path, target: Path) -> None:
    """Extract only regular bounded members into a newly created private directory."""
    with zipfile.ZipFile(source) as archive:
        names: set[str] = set()
        expanded = 0
        for member in archive.infolist():
            name = member.filename
            relative = PurePosixPath(name)
            mode = member.external_attr >> 16
            if (not name or "\\" in name or ":" in name or relative.is_absolute() or
                    any(part in (".", "..") for part in relative.parts) or stat.S_ISLNK(mode)):
                raise ValueError("Unsafe path in a build-input archive")
            folded = name.casefold()
            if folded in names:
                raise ValueError("Duplicate path in a build-input archive")
            names.add(folded)
            expanded += member.file_size
            if expanded > 300 * 1024 * 1024 or len(names) > 5000:
                raise ValueError("Build-input archive exceeds its extraction limits")
        for member in archive.infolist():
            destination = target.joinpath(*PurePosixPath(member.filename).parts)
            if member.is_dir():
                destination.mkdir(parents=True, exist_ok=True)
            else:
                destination.parent.mkdir(parents=True, exist_ok=True)
                with archive.open(member) as source_stream, destination.open("xb") as target_stream:
                    shutil.copyfileobj(source_stream, target_stream)


def output_inventory(directory: Path) -> dict[str, str]:
    files = sorted(directory.glob("*.dll"), key=lambda file: file.name.casefold())
    if not files or any(file.is_symlink() for file in files):
        raise ValueError("Generated game references are missing or unsafe")
    return {file.name: sha256(file) for file in files}


def reuse_output() -> bool:
    try:
        receipt_path = OUTPUT / "build-inputs.json"
        if OUTPUT.is_symlink() or receipt_path.is_symlink():
            return False
        receipt = json.loads(receipt_path.read_text(encoding="utf-8"))
        return receipt.get("spec") == SPEC and receipt.get("assemblies") == output_inventory(OUTPUT)
    except (OSError, ValueError, KeyError):
        return False


def prepare(quiet: bool = False) -> Path:
    CACHE.mkdir(parents=True, exist_ok=True)
    # A lock prevents two simultaneous builds from replacing one another's references.
    lock_path = CACHE / "prepare.lock"
    try:
        lock_fd = os.open(lock_path, os.O_CREAT | os.O_EXCL | os.O_WRONLY)
    except FileExistsError:
        raise RuntimeError("Another HAU reference preparation is running. If it was interrupted, remove artifacts/interop/prepare.lock and retry.")
    try:
        with os.fdopen(lock_fd, "w") as lock:
            lock.write(str(os.getpid()))
        # Validate all retained official inputs even when generated assemblies are cached.
        assets = {key: download_asset(key) for key in ASSETS}
        if reuse_output():
            if not quiet:
                print(f"Verified and reused {len(output_inventory(OUTPUT))} Interop 1.5.3 game references.")
            return OUTPUT
        with tempfile.TemporaryDirectory(prefix="prepare-", dir=CACHE) as temp:
            stage = Path(temp)
            for key, archive in assets.items():
                extract_archive(archive, stage / key)
            generated = stage / "references"
            command = ["dotnet", "--roll-forward", "Major", str(stage / "cli/net6.0/Il2CppInterop.CLI.dll"),
                       "generate", "--input", str(stage / "sdk/build"), "--output", str(generated),
                       "--unity", str(stage / "unity"), "--no-xref-cache"]
            result = subprocess.run(command, cwd=ROOT, text=True, capture_output=True, timeout=300)
            (CACHE / "generation.log").write_text(result.stdout + result.stderr, encoding="utf-8")
            if result.returncode != 0:
                raise RuntimeError("Il2CppInterop 1.5.3 reference generation failed; see artifacts/interop/generation.log")
            inventory = output_inventory(generated)
            required = ("Assembly-CSharp.dll", "UnityEngine.CoreModule.dll", "Il2Cppmscorlib.dll")
            if any(name not in inventory for name in required):
                raise RuntimeError("Generated reference set is incomplete")
            (generated / "build-inputs.json").write_text(json.dumps({"spec": SPEC, "assemblies": inventory}, indent=2) + "\n", encoding="utf-8")
            prior = CACHE / f"references-old-{os.getpid()}"
            if OUTPUT.exists():
                if OUTPUT.is_symlink():
                    raise ValueError("Generated reference directory cannot be a symlink")
                OUTPUT.replace(prior)
            try:
                generated.replace(OUTPUT)
            except BaseException:
                if prior.exists():
                    prior.replace(OUTPUT)
                raise
            if prior.exists():
                shutil.rmtree(prior)
            if not quiet:
                print(f"Prepared {len(inventory)} Interop 1.5.3 game references for Among Us 2026.9.29 / loader 788.")
            return OUTPUT
    finally:
        lock_path.unlink(missing_ok=True)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--quiet", action="store_true")
    args = parser.parse_args()
    try:
        prepare(args.quiet)
    except (OSError, ValueError, RuntimeError, subprocess.TimeoutExpired) as error:
        raise SystemExit(str(error))
