# 角色素材与生成记录

## v1.2.2 osu!mania 坐姿

像素游戏坐姿最终采用用户选定的第一张局部修正版；保留该张的头部、尾巴、翅膀、手部和跪姿轮廓。后续生成版本不用于发行。

2026-10-24 使用内置 ImageGen，分别以 `assets/anime.png` 和 `assets/pixel.png` 的左上角角色为身份与画风参考，生成透明独立素材 `assets/anime-mania.png`、`assets/pixel-mania.png`。提示重点是：同一名白发紫瞳龙娘，三分之二侧背坐姿，朝右看向画面外的游戏屏幕；保留角、翅膀、尾巴和裙装，腿自然收起，两臂从肩膀向前伸；不生成屏幕、键盘、文字或背景，不截断身体。像素风额外要求清晰像素边缘和块状调色。程序在右下角使用水平镜像，分轨屏与按键光效由代码绘制。

使用内置 ImageGen（非 API CLI）。生成日期：2026-10-23。

- `assets/anime.png`：二次元风透明图集，4 列 × 2 行。
- `assets/pixel.png`：像素风透明图集，4 列 × 2 行。

帧顺序：待机、眨眼、思考、等待、庆祝、出错、挥手、休息。程序按原图比例绘制；像素风采用最近邻插值。动画由姿态切换、眨眼和位移组合构成，不是逐帧骨骼动画。

社区设计参考：[GPT 龙娘图鉴](https://aiher.ai/gpt/)、[社区龙娘桌宠说明](https://github.com/Marco0431/x-dragon-girl-pet)。仅参考白发、紫瞳、龙角和龙尾等设计方向，未下载其美术资产。

## 二次元风提示词

Use case: stylized-concept. Asset type: production desktop companion sprite atlas, transparent PNG. Create ONE perfectly regular 4 columns x 2 rows sprite sheet on a square 1536x1536 transparent canvas, eight equal square cells, no visible grid. Every cell contains the SAME adorable chibi anime dragon girl, full body head to feet plus tail fully inside its cell, with generous transparent margins, equal size and identical ground baseline. Original fan-inspired AI companion: long flowing silver-white hair, two small pale gray curved dragon horns, expressive lavender-purple eyes, small pointed ears, white dragon tail, tiny pale lavender bat-shaped dragon wings, white and lavender modest layered fantasy dress with dark plum ribbon, little white boots, a simple violet crystal hair clip. Polished Japanese 2D chibi anime game illustration, crisp thin outlines, soft cel shading, beautiful consistent character design, 2.5 heads tall. Top row cells left to right: 1 neutral happy standing eyes open hands by sides; 2 same neutral pose eyes closed for blinking; 3 thinking holding a small closed lavender book, attentive eyes; 4 waiting for user input, one hand raised inquisitively. Bottom row: 5 celebration with delighted closed eyes and both arms up; 6 concerned error expression looking mildly worried hands clasped; 7 happy waving to user with one hand; 8 sleeping peacefully while standing hands together eyes closed. No text, no labels, no logos, no watermarks, no checkerboard, no backdrop, no ground shadow outside character. All eight characters separated, no overlap, exactly one character per cell. True transparent background. Match the size and baseline across cells precisely.

## 像素风提示词

Use case: stylized-concept. Production 2D desktop pet sprite atlas. Create one WIDE LANDSCAPE image aspect ratio 2:1, 4 columns and 2 rows, eight identical SQUARE cells, true transparent background, no visible grid. Each square cell contains ONE full-body chibi white-haired purple-eyed dragon girl centered with generous EMPTY TRANSPARENT MARGINS on every side. Consistent character: long white hair, pale gray curved dragon horns, lavender eyes, white dragon tail, tiny lavender wings, modest white and lavender dress with plum bow, violet crystal hair clip, white boots. PURE AUTHENTIC HAND-PLACED PIXEL ART, 64x64 logical pixels PER CELL displayed at integer scale; sharp chunky square pixels, hard edges, very restricted 20-color palette, delicate but genuinely pixelated 16-bit JRPG game sprite, no anti-aliasing, no smooth gradients, no painterly shading, no smooth outlines. Each character maximum 48x54 logical pixels within its 64x64 cell. Equal size and same baseline in all cells. Top row left to right: neutral standing eyes open; same neutral pose eyes closed blink; thinking holding a purple book; waiting with one hand raised. Bottom row: happy both hands raised celebration; concerned worried hands clasped; cheerful waving; peacefully sleeping eyes closed. All eight full-body characters complete and separate, no touching neighboring cells, no extra items, no lettering, no logos, no checkerboard. This must look unmistakably like crisp real pixel art, not a normal anime illustration with a pixel filter.

developed by WodeFafa

