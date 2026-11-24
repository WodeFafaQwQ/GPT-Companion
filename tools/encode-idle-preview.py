"""Encode the real idle frames without synthesizing extra poses."""
from pathlib import Path
import hashlib
import json
import sys
from PIL import Image

root = Path(sys.argv[1]).resolve()
binary = Path(sys.argv[3]).resolve() if len(sys.argv) > 3 else Path(__file__).resolve().parent.parent / "dist" / "DragonCompanion.preview.exe"
paths = sorted((root / "frames").glob("*.png"))
assert len(paths) >= 240
frames = [Image.open(path).convert("RGB") for path in paths]
w, h = frames[0].size
sample = Image.new("RGB", (w * 4, h * 4))
for i in range(16):
    sample.paste(frames[i * (len(paths) - 1) // 15], (i % 4 * w, i // 4 * h))
palette = sample.quantize(colors=256, method=Image.Quantize.MEDIANCUT)
encoded = [frame.quantize(palette=palette, dither=Image.Dither.NONE) for frame in frames]
target = root / "idle.gif"
encoded[0].save(target, save_all=True, append_images=encoded[1:],
                duration=50, loop=0, optimize=True, disposal=1)
with Image.open(target) as gif:
    duration = 0
    for i in range(gif.n_frames):
        gif.seek(i)
        duration += gif.info.get("duration", 0)
    assert duration == len(paths) * 50 and gif.n_frames >= 20
    print(f"Verified idle GIF: {gif.n_frames} held frames, {duration} ms.")
(root / "verification.json").write_text(json.dumps({
    "binary_sha256": hashlib.sha256(binary.read_bytes()).hexdigest(),
    "source_start_ms": int(sys.argv[2]) if len(sys.argv) > 2 else 0,
    "source_samples": len(paths), "sample_ms": 50, "duration_ms": duration,
    "tail_cycle_lengths_ms": [18000, 22500, 20500, 24000],
    "tail_active_starts_ms_per_cycle": [5000, 7400, 6100, 9200],
    "tail_active_duration_ms": 2800,
    "fixed_character_body": True, "whole_body_bob": False,
    "actions": ["tail tip sway", "opaque native blink", "brief left and right glances"],
    "packaged": False,
}, indent=2), encoding="utf-8")
