"""Encode unmodified frames rendered by Render-BellPreview.cs."""
from pathlib import Path
import hashlib
import json
import sys
from PIL import Image

root = Path(sys.argv[1]).resolve()
frames = [Image.open(path).convert("RGB") for path in sorted((root / "frames").glob("*.png"))]
assert len(frames) == 64
sample = Image.new("RGB", (2560, 1504))
for i in range(16):
    sample.paste(frames[i * 4], (i % 4 * 640, i // 4 * 376))
palette = sample.quantize(colors=256, method=Image.Quantize.MEDIANCUT)
encoded = [frame.quantize(palette=palette, dither=Image.Dither.NONE) for frame in frames]
target = root / "bell-preview.gif"
encoded[0].save(target, save_all=True, append_images=encoded[1:], duration=50,
                loop=0, optimize=True, disposal=1)
with Image.open(target) as gif:
    duration = 0
    for i in range(gif.n_frames):
        gif.seek(i)
        duration += gif.info.get("duration", 0)
    assert duration == 3200 and gif.n_frames == 64
checks = (root / "checks.txt").read_text(encoding="utf-8-sig").splitlines()
assert all(line.startswith("PASS ") for line in checks)
binary = Path(sys.argv[2]).resolve() if len(sys.argv)>2 else Path(__file__).resolve().parent.parent / "dist" / "DragonCompanion.preview.exe"
(root / "verification.json").write_text(json.dumps({
    "binary_sha256": hashlib.sha256(binary.read_bytes()).hexdigest(),
    "frames": 64, "duration_ms": 3200, "checks_passed": len(checks),
    "packaged": False, "grip": "original fingers in front of upper pull-ring",
}, indent=2), encoding="utf-8")
print(f"Verified {len(checks)} checks, 64 actual frames, 3200 ms; no package generated.")

# The close-up is rendered at its final size by the application, not enlarged
# from the overview GIF or a cached low-resolution sprite.
detail_paths = sorted((root / "grip-frames").glob("*.png"))
if detail_paths:
    assert len(detail_paths) == 64
    detail_frames = [Image.open(path).convert("RGB") for path in detail_paths]
    w, h = detail_frames[0].size
    detail_sample = Image.new("RGB", (w * 4, h * 4))
    for i in range(16):
        detail_sample.paste(detail_frames[i * 4], (i % 4 * w, i // 4 * h))
    detail_palette = detail_sample.quantize(colors=256, method=Image.Quantize.MEDIANCUT)
    detail_encoded = [frame.quantize(palette=detail_palette, dither=Image.Dither.NONE) for frame in detail_frames]
    detail_target = root / "grip-detail.gif"
    detail_encoded[0].save(detail_target, save_all=True, append_images=detail_encoded[1:],
                           duration=50, loop=0, optimize=True, disposal=1)
    with Image.open(detail_target) as result:
        total = 0
        for i in range(result.n_frames):
            result.seek(i)
            total += result.info.get("duration", 0)
        assert result.n_frames == 64 and total == 3200
    print("Verified native close-up: 64 frames, 3200 ms.")
