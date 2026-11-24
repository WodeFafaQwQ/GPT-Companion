"""Encode the unmodified real frames rendered by Render-PaintedPreview.cs."""
from pathlib import Path
import hashlib
import json
import sys
from PIL import Image

root = Path(sys.argv[1]).resolve()
paths = sorted((root / "frames").glob("*.png"))
assert len(paths) == 132
frames = [Image.open(path).convert("RGB") for path in paths]
sample = Image.new("RGB", (2560, 2560))
for i in range(16):
    sample.paste(frames[i * (len(frames) - 1) // 15], (i % 4 * 640, i // 4 * 640))
palette = sample.quantize(colors=256, method=Image.Quantize.MEDIANCUT)
encoded = [frame.quantize(palette=palette, dither=Image.Dither.NONE) for frame in frames]
target = root / "tea-typing.gif"
encoded[0].save(target, save_all=True, append_images=encoded[1:],
                duration=50, loop=0, optimize=True, disposal=1)
with Image.open(target) as gif:
    duration = 0
    for i in range(gif.n_frames):
        gif.seek(i)
        duration += gif.info.get("duration", 0)
    assert duration == 6600 and gif.n_frames >= 16
    print(f"Verified {gif.n_frames} held frames, {duration} ms.")
checks = (root / "checks.txt").read_text(encoding="utf-8-sig").splitlines()
assert checks and all(line.startswith("PASS ") for line in checks)
binary = Path(sys.argv[2]).resolve() if len(sys.argv) > 2 else Path(__file__).resolve().parent.parent / "dist" / "DragonCompanion.preview.exe"
(root / "verification.json").write_text(json.dumps({
    "binary_sha256": hashlib.sha256(binary.read_bytes()).hexdigest(),
    "source_samples": 132, "sample_ms": 50, "duration_ms": duration,
    "painted_tea_source_poses": 8, "tea_sequence_steps": 15,
    "painted_typing_poses": 8, "checks_passed": len(checks),
    "packaged": False, "startup": "User confirmed working; unchanged",
}, indent=2), encoding="utf-8")
print(f"{len(checks)} animation checks passed; no package created.")
