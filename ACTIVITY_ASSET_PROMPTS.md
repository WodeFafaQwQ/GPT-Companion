# GPT 八帧动作素材与提示词

生成方式：内置 image_gen 工具（imagegen 技能），透明背景；没有使用 API、密钥或第三方素材。参考为项目原有 anime.png／pixel.png。先完成四帧差分与留白修订，再扩展成最终八帧。最终 PNG 保留生成的透明度，软件读取时分离各帧及相邻碎片，不改写原始 PNG。

最终素材路径（相对源码根目录）：

- assets/pixel-daily-8.png：看书翻页、伸懒腰、喝茶、抱枕休息，八列四行。
- assets/anime-daily-8.png：相同四组的二次元画风。
- assets/pixel-tasks-8.png：专注工作、等待回复、完成庆祝、日程提醒，八列四行。
- assets/anime-tasks-8.png：相同四组的二次元画风。

每种画风共 64 张原画，两种画风共 128 张。初稿仅存本地 tests/activity-artwork-drafts，不进入最终发行素材。运行时根据真实透明行间隔切图，不能假定四行等高。

最终四次调用均使用 transparent_background=true，各附对应风格／分类的四帧修订图作为 referenced_image_paths；未使用 num_last_images_to_include。以下通用段、风格段、分类段、结尾段按顺序拼接，构成每次最终调用的完整提示词。

## 通用段

Create the final EIGHT-FRAME animation atlas from this reference four-frame atlas. LANDSCAPE CANVAS, 2:1 aspect ratio, ideally 3200x1600. EXACTLY EIGHT EQUAL COLUMNS and FOUR EQUAL ROWS (32 individual full-body sprites). Do NOT return four columns. Expand each of the reference's four rows into EIGHT consecutive smooth animation keyframes. Each row retains its reference action and style. Ref frame 1 becomes new col1; smoothly transition to ref frame2 in cols2-3, ref frame3 in cols4-5, ref frame4 in cols6-7; col8 returns toward col1 to make a seamless cycle. Every sprite must be the same scale, face, clothing and character identity. FIX torso and head placement and foot baseline across all eight frames of each row: ONLY the hands/arms, relevant prop/page and eyelids change gradually. No jiggling camera, no leg changes. Keep arms fully connected.

## 风格段

像素风：Authentic clean square-pixel 16bit pixel art matching reference.

二次元风：Smooth clean chibi anime cel illustration matching reference.

## 分类段

日常：Row1 reads an open PURPLE BOOK with an unmistakable IVORY PAGE TURN: first frame reading, next fingers lift page, next page rises, middle curved page crosses spine, next page settles to left, final returns to reading. No hand raised to mouth. Row2 slow stretch with both arms rising from chest to overhead and returning, not waving. Row3 slowly raises LILAC MUG to lips, sip, lowers to chest. Row4 hugs lavender pillow with slow sleepy blink, no sitting/standing switches.

任务：Row1 hands alternately typing on a fixed lavender keyboard at waist; row2 waiting with small QUESTION MARK card and fingers at chin, curious glance and blink; row3 gently raises gold star trophy to celebrate and lowers it, smiles; row4 small notepad CHECK MARK and bell gently tilting with wrist.

## 结尾段

Each character INCLUDING entire hair curl, horns, wing tips, tail, boots and props is contained in the CENTER 70% of its own cell width and height, with at least 15% fully transparent padding all around EVERY cell. No clipping on final row or top edge. Large clean empty gutters. Genuine transparent alpha, no opaque white speckles, no magenta/blue key-color fringe, no shadow, no background, no grid, no borders, no captions, no lettering apart from ? or check icon on props. Keep all 32 sprites distinct, fully complete and equally aligned.

## 稳定修正的运行方式

本次没有重新生成或改写 PNG。原画差分在运行时只提供局部手臂、道具区域，人物基底使用固定帧；配准后的局部区域合成到固定人物上。阅读改为固定持书姿势与代码绘制的独立书页，因此翻页不再依赖原画中未完成的翻书差分。此为代码原生动画层，不是用其他图像工具修改角色原画。

## 预览

verification/activities-pixel.gif、verification/activities-anime.gif 使用最终 EXE 的实际 Art.Draw 帧编码，每段六秒；reading-fixed.gif 使用同一播放速度，将起始相位移到 2.5 秒处以更快看到翻页；verification/activity-preview.html 为本地查看入口。tools/render-activity-preview.py 只将软件渲染的 PNG 编码为 GIF，不绘制或修改动作原画。

developed by WodeFafa

v1.2.4 的书页位置修正与阅读循环使用代码原生渲染，不修改或重新生成 PNG；校准两个书本平面，并按每翻三页停留 6 秒的节奏复用原始第 3 格持书思考姿势。新预览为 verification/reading-aligned.gif。

## v1.2.5 书页与姿势衔接

书页从最上层纸张略上方开始，固定在上缘凹槽，内沿连续连到封面书脊。保留旧思考原画，使用面部、书本／手和下身的对应网格形变与预乘透明合成，进入／退出各 700 ms、15 个画面，避免直接淡化出现双脸。原动作稳定保持 6000 ms；缓存随延后动作预热生成。未生成新整图，其他七组动作沿用已固定人物的八帧版本。预览由最终 Art.Draw 20 Hz 采样。
