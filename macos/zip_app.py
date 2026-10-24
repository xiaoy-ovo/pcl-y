#!/usr/bin/env python3
"""
打包 macOS .app 为 zip，并写入正确的 Unix 权限位。

Windows 的 zip / Compress-Archive 不会保存可执行位，解压后
Contents/MacOS/PCL-Y 会是 0644，双击直接报 "The application cannot be opened"。
这里手工写 external_attr，保证可执行文件解压后是 0755。

用法： python zip_app.py <app_dir> <output_zip>
"""
import os
import stat
import sys
import zipfile


def unix_mode(path: str) -> int:
    """可执行文件/脚本 0755，其余 0644，目录 0755。"""
    if os.path.isdir(path):
        return 0o755
    name = os.path.basename(path)
    if name in ("PCL-Y", "PclYMac"):
        return 0o755
    if name.endswith((".sh", ".command")):
        return 0o755
    try:
        if os.stat(path).st_mode & stat.S_IXUSR:
            return 0o755
    except OSError:
        pass
    return 0o644


def main() -> int:
    if len(sys.argv) != 3:
        print(__doc__)
        return 2

    app_dir, out_zip = sys.argv[1], sys.argv[2]
    if not os.path.isdir(app_dir):
        print(f"not a directory: {app_dir}", file=sys.stderr)
        return 1

    app_dir = os.path.abspath(app_dir)
    parent = os.path.dirname(app_dir)
    root_name = os.path.basename(app_dir)

    if os.path.exists(out_zip):
        os.remove(out_zip)

    file_count = 0
    with zipfile.ZipFile(out_zip, "w", zipfile.ZIP_DEFLATED, compresslevel=9) as zf:
        # 先写目录条目，保留空目录结构
        for dirpath, dirnames, filenames in os.walk(app_dir):
            dirnames.sort()
            filenames.sort()

            rel_dir = os.path.relpath(dirpath, parent).replace(os.sep, "/")
            if rel_dir != ".":
                info = zipfile.ZipInfo(rel_dir + "/")
                info.external_attr = (stat.S_IFDIR | 0o755) << 16
                info.create_system = 3  # Unix
                zf.writestr(info, b"")

            for name in filenames:
                full = os.path.join(dirpath, name)
                rel = os.path.relpath(full, parent).replace(os.sep, "/")
                mode = unix_mode(full)

                info = zipfile.ZipInfo(rel)
                info.create_system = 3  # Unix -> 解压端按 external_attr 还原权限
                info.external_attr = (stat.S_IFREG | mode) << 16
                info.compress_type = zipfile.ZIP_DEFLATED

                with open(full, "rb") as src:
                    zf.writestr(info, src.read())
                file_count += 1

    size_mb = os.path.getsize(out_zip) / (1024 * 1024)
    print(f"    packed {file_count} files -> {out_zip} ({size_mb:.1f} MB)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
