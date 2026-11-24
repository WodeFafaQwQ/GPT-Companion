"""Encode the app's actual rendered animation frames; do not synthesize poses."""
from pathlib import Path
import sys
from PIL import Image

folder = Path(sys.argv[1]).resolve()
for style in ("pixel", "anime"):
    paths = sorted((folder / "activity-frames" / style).glob("*.png"))
    if len(paths) not in (120, 200):
        raise RuntimeError(f"Expected 120 or 200 application-rendered frames: {style}")
    frames = [Image.open(path).convert("RGB") for path in paths]
    width, height = frames[0].size
    samples = Image.new("RGB", (width * 4, height * 4))
    for i in range(16):
        samples.paste(frames[i * 7], ((i % 4) * width, (i // 4) * height))
    palette = samples.quantize(colors=256, method=Image.Quantize.MEDIANCUT)
    encoded = [frame.quantize(palette=palette, dither=Image.Dither.NONE) for frame in frames]
    destination = folder / f"activities-{style}.gif"
    encoded[0].save(destination, save_all=True, append_images=encoded[1:],
                    duration=50, loop=0, optimize=True, disposal=1)
    with Image.open(destination) as result:
        duration = 0
        for i in range(result.n_frames):
            result.seek(i)
            duration += result.info.get("duration", 0)
        if duration != len(paths) * 50 or result.n_frames < 30:
            raise RuntimeError(f"Invalid GIF timing/frame sequence: {destination}")
        print(f"{destination}: {result.n_frames} frames, {duration} ms, {destination.stat().st_size} bytes")
    for frame in frames + encoded:
        frame.close()
    palette.close()
    samples.close()
(folder / "activity-preview.html").write_text('''<!doctype html><html lang="zh-CN"><meta charset="utf-8">
<title>GPT 动作预览</title><style>body{margin:32px;background:#faf7ff;color:#302541;font:16px "Microsoft YaHei UI",sans-serif}main{max-width:1000px;margin:auto}img{width:100%;max-width:736px;border:1px solid #d8caef;border-radius:18px}p{line-height:1.8}footer{color:#82758e;margin-top:24px}</style>
<main><h1>GPT · 动作预览</h1><p>上排：看书翻页、喝茶、抱枕休息。下排：专注工作、等待回复、完成庆祝、日程提醒。喝茶使用连接完整的原画杯子与手袖帧，敲键盘固定肩袖、只轻动手指；抱枕休息只眨眼，庆祝的小花和彩带按时间连续运动。以软件实际节奏播放；实际待机随机安排，任务动作由对应状态触发。</p>
<h2>像素风</h2><img src="activities-pixel.gif" alt="像素风动作预览"><h2>二次元风</h2><img src="activities-anime.gif" alt="二次元风动作预览"><footer>developed by WodeFafa</footer></main></html>''', encoding="utf-8")

detail_paths = sorted((folder / "preferences-frames").glob("*.png"))
if detail_paths:
    if len(detail_paths) != 200:
        raise RuntimeError("Close-up preview must contain 200 real 50 ms frames")
    frames = [Image.open(path).convert("RGB") for path in detail_paths]
    width, height = frames[0].size
    sample = Image.new("RGB", (width * 4, height * 4))
    for i in range(16):
        sample.paste(frames[i * 199 // 15], ((i % 4) * width, (i // 4) * height))
    palette = sample.quantize(colors=256, method=Image.Quantize.MEDIANCUT)
    encoded = [frame.quantize(palette=palette, dither=Image.Dither.NONE) for frame in frames]
    target = folder / "action-details.gif"
    encoded[0].save(target, save_all=True, append_images=encoded[1:], duration=50,
                    loop=0, optimize=True, disposal=1)
    with Image.open(target) as result:
        total = 0
        for i in range(result.n_frames):
            result.seek(i)
            total += result.info.get("duration", 0)
        if total != 10000 or result.n_frames < 150:
            raise RuntimeError("Close-up lost continuous particle animation")
        print(f"{target}: {result.n_frames} frames, {total} ms")
    for frame in frames + encoded:
        frame.close()
    palette.close()
    sample.close()
    html = folder / "activity-preview.html"
    html.write_text(html.read_text(encoding="utf-8").replace("<footer>",
        '<h2>工作敲键、端杯喝茶、欢呼小跳、铃铛摇动</h2><img src="action-details.gif" alt="动作细节"><footer>'), encoding="utf-8")

# Render-ReadingPreview.cs writes only changing keyframes plus real hold times.
reading_folder = folder / "reading-closeup"
timing_file = reading_folder / "timings.csv"
if timing_file.exists():
    import csv
    with timing_file.open(encoding="utf-8-sig", newline="") as stream:
        timing = list(csv.reader(stream))
    images = [Image.open(reading_folder / row[0]).convert("RGB") for row in timing]
    durations = [int(row[1]) for row in timing]
    width, height = images[0].size
    sample = Image.new("RGB", (width * 4, height * 4))
    for i in range(16):
        index = i * (len(images) - 1) // 15
        sample.paste(images[index], ((i % 4) * width, (i // 4) * height))
    palette = sample.quantize(colors=256, method=Image.Quantize.MEDIANCUT)
    encoded = [im.quantize(palette=palette, dither=Image.Dither.NONE) for im in images]
    target = folder / "reading-aligned.gif"
    encoded[0].save(target, save_all=True, append_images=encoded[1:], duration=durations, loop=0, optimize=True, disposal=1)
    with Image.open(target) as result:
        total = 0
        for i in range(result.n_frames):
            result.seek(i)
            total += result.info.get("duration", 0)
        if total != sum(durations) or result.n_frames < 25 or not any(row[2] == "thinking" and int(row[1]) == 6000 for row in timing):
            raise RuntimeError("Reading cycle lost its page turns or six-second thinking stage")
        print(f"{target}: {result.n_frames} frames, {total} ms, original thinking stage retained")
    for im in images + encoded:
        im.close()
    sample.close()
    palette.close()
    html = folder / "activity-preview.html"
    text = html.read_text(encoding="utf-8").replace("<footer>", '<h2>翻页 → 原动作思考 → 继续翻页</h2><img src="reading-aligned.gif" alt="完整阅读循环"><footer>')
    html.write_text(text, encoding="utf-8")
