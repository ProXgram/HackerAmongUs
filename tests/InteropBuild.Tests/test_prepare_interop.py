"""Build-input regression fixtures; no network or installed game is used."""
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import zipfile

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location("prepare_interop", ROOT / "scripts/prepare-interop.py")
prepare_interop = importlib.util.module_from_spec(spec)
spec.loader.exec_module(prepare_interop)


class BuildInputs(unittest.TestCase):
    def test_pinned_asset_rejects_changed_bytes_and_truncation(self):
        with tempfile.TemporaryDirectory() as temporary:
            asset = Path(temporary) / "input.zip"
            asset.write_bytes(b"original")
            expected = prepare_interop.sha256(asset)
            prepare_interop.require_asset(asset, expected, 8)
            asset.write_bytes(b"modified")
            with self.assertRaises(ValueError):
                prepare_interop.require_asset(asset, expected, 8)
            asset.write_bytes(b"short")
            with self.assertRaises(ValueError):
                prepare_interop.require_asset(asset, expected, 8)

    def archive_fixture(self, names):
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        folder = Path(temporary.name)
        source = folder / "input.zip"
        with zipfile.ZipFile(source, "w") as archive:
            for name in names:
                # ZipInfo normally normalizes Windows separators; construct the
                # raw member name so the malformed archive fixture keeps them.
                member = zipfile.ZipInfo()
                member.filename = name
                archive.writestr(member, b"fixture")
        return source, folder / "expanded"

    def test_archive_extracts_normal_members(self):
        source, destination = self.archive_fixture(["net6.0/tool.dll", "LICENSE"])
        prepare_interop.extract_archive(source, destination)
        self.assertEqual((destination / "net6.0/tool.dll").read_bytes(), b"fixture")

    def test_archive_rejects_traversal_before_writing_any_member(self):
        for unsafe in ("../escape.dll", "/absolute.dll", "C:/outside.dll", "dir\\..\\outside.dll"):
            with self.subTest(path=unsafe):
                source, destination = self.archive_fixture(["valid.dll", unsafe])
                with self.assertRaises(ValueError):
                    prepare_interop.extract_archive(source, destination)
                self.assertFalse(destination.exists())

    def test_archive_rejects_case_collisions(self):
        source, destination = self.archive_fixture(["Tool.dll", "tool.dll"])
        with self.assertRaises(ValueError):
            prepare_interop.extract_archive(source, destination)
        self.assertFalse(destination.exists())

    def test_cache_rejects_changed_added_removed_assemblies(self):
        with tempfile.TemporaryDirectory() as temporary:
            output = Path(temporary)
            proxy = output / "Assembly-CSharp.dll"
            proxy.write_bytes(b"verified assembly fixture")
            receipt = {"spec": prepare_interop.SPEC, "assemblies": prepare_interop.output_inventory(output)}
            (output / "build-inputs.json").write_text(json.dumps(receipt), encoding="utf-8")
            with patch.object(prepare_interop, "OUTPUT", output):
                self.assertTrue(prepare_interop.reuse_output())
                proxy.write_bytes(b"altered assembly fixture")
                self.assertFalse(prepare_interop.reuse_output())
                proxy.write_bytes(b"verified assembly fixture")
                extra = output / "unexpected.dll"
                extra.write_bytes(b"unexpected")
                self.assertFalse(prepare_interop.reuse_output())
                extra.unlink()
                proxy.unlink()
                self.assertFalse(prepare_interop.reuse_output())

    def test_cache_rejects_old_toolchain_and_malformed_receipt(self):
        with tempfile.TemporaryDirectory() as temporary:
            output = Path(temporary)
            (output / "Assembly-CSharp.dll").write_bytes(b"fixture")
            receipt_path = output / "build-inputs.json"
            receipt = {"spec": {**prepare_interop.SPEC, "interopVersion": "1.5.1"},
                       "assemblies": prepare_interop.output_inventory(output)}
            receipt_path.write_text(json.dumps(receipt), encoding="utf-8")
            with patch.object(prepare_interop, "OUTPUT", output):
                self.assertFalse(prepare_interop.reuse_output())
                receipt_path.write_text("partial invalid JSON", encoding="utf-8")
                self.assertFalse(prepare_interop.reuse_output())


if __name__ == "__main__":
    unittest.main()
