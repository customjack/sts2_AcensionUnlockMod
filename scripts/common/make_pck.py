#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import os
import re
import shutil
import subprocess
import tempfile
import time
from pathlib import Path


def run(cmd: list[str], cwd: Path | None = None, quiet: bool = False) -> None:
    if not quiet:
        print("+", " ".join(cmd))
    subprocess.run(cmd, cwd=str(cwd) if cwd else None, check=True)


def find_godot(project_root: Path) -> str:
    env_exe = os.environ.get("GODOT_EXE")
    if env_exe and Path(env_exe).exists():
        return env_exe

    local_linux = project_root / ".tools" / "godot" / "godot"
    if local_linux.exists():
        return str(local_linux)

    for candidate in ("godot4", "godot"):
        exe = shutil.which(candidate)
        if exe:
            return exe

    raise SystemExit("Godot executable not found. Run setup_godot_cli or set GODOT_EXE.")


def resolve_image_path(project_root: Path) -> Path | None:
    if os.environ.get("MOD_IMAGE_PATH"):
        path = Path(os.environ["MOD_IMAGE_PATH"]).expanduser().resolve()
        return path if path.exists() else None

    top_level = project_root / "mod_image.png"
    if top_level.exists():
        return top_level

    resources_icon = project_root / "resources" / "icon.png"
    if resources_icon.exists():
        return resources_icon

    return None


def read_manifest(manifest_path: Path) -> dict:
    return json.loads(manifest_path.read_text(encoding="utf-8"))


def resolve_mod_id(manifest: dict) -> str:
    mod_id = (manifest.get("id") or manifest.get("pck_name") or manifest.get("name") or "").strip()
    if not mod_id:
        raise SystemExit("Manifest must declare 'id' (or legacy 'pck_name').")
    return mod_id


def generate_import_artifacts(godot_exe: str, mod_id: str, source_image: Path, temp_prefix: str) -> tuple[Path, Path, str] | None:
    with tempfile.TemporaryDirectory() as temp_dir_raw:
        temp_dir = Path(temp_dir_raw)
        mod_dir = temp_dir / mod_id
        mod_dir.mkdir(parents=True, exist_ok=True)

        temp_image = mod_dir / "mod_image.png"
        shutil.copy2(source_image, temp_image)

        (temp_dir / "project.godot").write_text(
            "config_version=5\n\n"
            "[application]\n"
            'config/name="ModImageImport"\n'
            'config/features=PackedStringArray("4.5")\n'
            f'config/icon="res://{mod_id}/mod_image.png"\n',
            encoding="utf-8",
        )

        run([godot_exe, "--headless", "--editor", "--quit", "--path", str(temp_dir)], quiet=True)

        import_file = mod_dir / "mod_image.png.import"
        if not wait_for_file(import_file):
            return None

        import_text = import_file.read_text(encoding="utf-8")
        ctex_rel_path = parse_ctex_path(import_text)
        if not ctex_rel_path:
            return None

        ctex_path = temp_dir / ctex_rel_path
        if not wait_for_file(ctex_path):
            return None

        stable_root = Path(tempfile.mkdtemp(prefix=f"{temp_prefix}_import_"))
        stable_import = stable_root / "mod_image.png.import"
        stable_ctex = stable_root / "mod_image.ctex"
        shutil.copy2(import_file, stable_import)
        shutil.copy2(ctex_path, stable_ctex)
        return stable_import, stable_ctex, ctex_rel_path


def wait_for_file(path: Path, timeout_seconds: float = 5.0) -> bool:
    deadline = time.time() + timeout_seconds
    while time.time() < deadline:
        try:
            if path.exists() and path.stat().st_size > 0:
                return True
        except OSError:
            pass
        time.sleep(0.1)
    return path.exists()


def parse_ctex_path(import_text: str) -> str | None:
    path_match = re.search(r'^path="res://([^"]+\.ctex)"', import_text, flags=re.MULTILINE)
    if path_match:
        return path_match.group(1)

    deps_match = re.search(r'dest_files=\["res://([^"]+\.ctex)"\]', import_text)
    if deps_match:
        return deps_match.group(1)

    return None


def main() -> None:
    parser = argparse.ArgumentParser(description="Pack a STS2 mod PCK")
    parser.add_argument("out_pck", nargs="?", default="")
    args = parser.parse_args()

    script_dir = Path(__file__).resolve().parent
    project_root = script_dir.parent.parent

    manifest_path = Path(os.environ.get("MANIFEST_PATH", str(project_root / "mod_manifest.json"))).resolve()
    if not manifest_path.exists():
        raise SystemExit(f"Missing manifest: {manifest_path}")

    manifest = read_manifest(manifest_path)
    mod_id = resolve_mod_id(manifest)
    out_pck = Path(args.out_pck).resolve() if args.out_pck else (project_root / f"{mod_id}.pck").resolve()
    dist_dir = project_root / "dist" / mod_id
    root_pck = (project_root / f"{mod_id}.pck").resolve()
    root_json = (project_root / f"{mod_id}.json").resolve()

    godot_exe = find_godot(project_root)
    image_path = resolve_image_path(project_root)

    out_pck.parent.mkdir(parents=True, exist_ok=True)
    dist_dir.mkdir(parents=True, exist_ok=True)

    pack_script = script_dir / "pack_pck.gd"
    cmd = [godot_exe, "--headless", "--path", str(project_root), "--script", str(pack_script), "--", str(out_pck), str(manifest_path)]

    cleanup_dir: Path | None = None
    try:
        if image_path and image_path.exists():
            cmd.append(str(image_path))

            import_bundle = generate_import_artifacts(
                godot_exe,
                mod_id,
                image_path,
                temp_prefix=project_root.name.lower().replace(" ", "_")
            )
            if import_bundle:
                import_path, ctex_path, ctex_rel = import_bundle
                cleanup_dir = import_path.parent
                cmd.extend([str(import_path), str(ctex_path), ctex_rel])
            else:
                print("Warning: Could not generate import artifacts. Mod image may fail to display.")

        print(f"Packing PCK with: {godot_exe}")
        run(cmd)

        if out_pck != root_pck:
            shutil.copy2(out_pck, root_pck)
        shutil.copy2(out_pck, dist_dir / f"{mod_id}.pck")
        shutil.copy2(manifest_path, dist_dir / f"{mod_id}.json")
        shutil.copy2(manifest_path, root_json)

        stale_manifest = dist_dir / "mod_manifest.json"
        if stale_manifest.exists():
            stale_manifest.unlink()

        print("PCK created:")
        print(f"  {out_pck}")
        print("Staged in:")
        print(f"  {dist_dir / f'{mod_id}.pck'}")
        print(f"  {dist_dir / f'{mod_id}.json'}")
    finally:
        if cleanup_dir and cleanup_dir.exists():
            shutil.rmtree(cleanup_dir, ignore_errors=True)


if __name__ == "__main__":
    main()
