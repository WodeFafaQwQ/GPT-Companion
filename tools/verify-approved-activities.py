"""Compare actual rendered RGBA bytes, including RGB when alpha is identical."""
from pathlib import Path
import json
import sys
from PIL import Image

baseline, current = (Path(value).resolve() for value in sys.argv[1:3])
freeze_tea = "--all" in sys.argv[3:]
regions = {
    "pixel_tea": (0, 0, 320, 300),
    "pixel_typing": (320, 0, 640, 300),
    "anime_typing": (320, 300, 640, 600),
}
if freeze_tea:
    regions["anime_tea"] = (0, 300, 320, 600)
paths = sorted((current / "frames").glob("*.png"))
assert len(paths) == 132
changed_tea = 0
for path in paths:
    with Image.open(baseline / "frames" / path.name) as old, Image.open(path) as new:
        for name, box in regions.items():
            assert old.crop(box).convert("RGBA").tobytes() == new.crop(box).convert("RGBA").tobytes(), (name, path.name)
        box = (0, 300, 320, 600)
        changed_tea += old.crop(box).convert("RGBA").tobytes() != new.crop(box).convert("RGBA").tobytes()
if freeze_tea:
    assert changed_tea == 0, "Approved anime tea must remain unchanged."
else:
    assert changed_tea > 0, "Anime tea renderer must actually display the changed coverage."
metadata_path = current / "verification.json"
metadata = json.loads(metadata_path.read_text(encoding="utf-8"))
metadata["approved_rgba_byte_identical_samples"] = {name: len(paths) for name in regions}
metadata["anime_tea_changed_samples"] = changed_tea
metadata["approved_baseline"] = str(baseline)
metadata_path.write_text(json.dumps(metadata, indent=2), encoding="utf-8")
print(f"Verified {len(paths) * len(regions)} approved RGBA crops unchanged; anime tea updated in {changed_tea} samples.")
