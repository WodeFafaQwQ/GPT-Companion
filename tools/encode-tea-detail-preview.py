"""Encode the real renderer's full-body / native wrist-detail PNG samples."""
from pathlib import Path
import json
import sys
from PIL import Image

root = Path(sys.argv[1]).resolve()
paths = sorted((root / "detail-frames").glob("*.png"))
assert len(paths) == 132
frames = [Image.open(path).convert("RGB") for path in paths]
assert all(frame.size == (640, 400) for frame in frames)
sample = Image.new("RGB", (2560, 1600))
for i in range(16):
    sample.paste(frames[i * 131 // 15], (i % 4 * 640, i // 4 * 400))
palette = sample.quantize(colors=256, method=Image.Quantize.MEDIANCUT)
encoded = [frame.quantize(palette=palette, dither=Image.Dither.NONE) for frame in frames]
expected_runs = 1 + sum(a.tobytes() != b.tobytes() for a, b in zip(encoded, encoded[1:]))
target = root / "anime-tea.gif"
encoded[0].save(target, save_all=True, append_images=encoded[1:], duration=50,
                loop=0, optimize=True, disposal=1)
with Image.open(target) as gif:
    duration = 0
    for i in range(gif.n_frames):
        gif.seek(i)
        duration += gif.info.get("duration", 0)
    assert duration == 6600 and gif.n_frames == expected_runs
    count = gif.n_frames
metadata_path = root / "verification.json"
metadata = json.loads(metadata_path.read_text(encoding="utf-8"))
metadata["tea_detail"] = {"file": target.name, "source_samples": 132,
                        "held_frames": count, "duration_ms": duration,
                        "closeup": "unmodified actual renderer crop, enlarged with nearest sampling"}
metadata_path.write_text(json.dumps(metadata, indent=2), encoding="utf-8")
print(f"Verified anime tea detail GIF: {count} held frames, {duration} ms.")
