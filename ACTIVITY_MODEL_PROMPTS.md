# 独立动作模型素材与生成提示词

生成日期：2026-10-26。使用内置 image_gen（imagegen 技能）的参考图编辑模式，transparent_background=true。共两次独立生成，没有使用 CLI/API 回退。

项目保存位置：
- 像素风：assets/pixel-activity-model-v1.png
- 二次元风：assets/anime-activity-model-v1.png

参考图：tests/action-diagnostics-motion-r5/pixel-tea-0.png 与 anime-tea-0.png，分别只用于同画风的角色身份和画风参考。

保留生成原件：
- assets/generated-sources/exec-a60284d5-c653-4827-9916-bccb1df1a518.png
- assets/generated-sources/exec-ee189ba1-017d-4ddc-84ae-2d2ba26fd3ec.png

## 两次生成共同提示词

Use case: identity-preserve and precise-object-edit. Asset type: production 2D articulated desktop-pet model atlas, NOT animation frames. Reference image is the exact character/style to preserve. Make ONE transparent 4-column x 2-row atlas with exactly EIGHT complete isolated parts in eight equal cells, no labels, no grid. Each part must remain fully inside the central 65% of its own cell, with generous completely transparent padding. Image around 2048x1024 landscape. Maintain the reference's chibi white-haired purple-eyed dragon girl, same face, two dark dragon horns, purple gem, purple wings, curved dragon tail, white/lilac dress, plum chest bow and boots.

READING ORDER EXACTLY:
top-left (cell 0): entire front-facing character body, SAME large-head chibi proportions as reference, full horns/hair/face/wings/tail/skirt/legs/feet. REMOVE mug, both forearms/sleeves and BOTH HANDS completely. Leave clean intact chest, bow and dress visible. Upper arms hidden behind body/hair so no protruding fingers or old props. This is the fixed background body layer for separate articulated arms; never draw dark holes or circular rig markers.
top-second (cell 1): isolated LEFT bell-shaped white and lilac forearm SLEEVE only, elbow end at TOP, cuff/wrist opening BOTTOM, vertical neutral orientation, rounded cloth, plum trim and tiny gold detail; NO hand attached, NO other body.
top-third (cell 2): isolated mirrored RIGHT forearm sleeve, same length/scale/material, elbow TOP cuff BOTTOM, NO hand.
top-right (cell 3): standalone small lavender computer keyboard seen slightly from above/front, realistic rows of small white square keys, gently rounded case. NO hands.
bottom-left (cell 4): isolated cute LEFT chibi hand, rounded small neutral fingers together, thumb on inner/right side, short wrist at TOP, fingers toward BOTTOM, soft peach skin, no sleeve, no arm, no floating extra finger.
bottom-second (cell 5): matching isolated RIGHT hand mirrored, short wrist at TOP and fingers BOTTOM, thumb on inner/left side.
bottom-third (cell 6): standalone whole lavender ceramic tea mug, white star on front, solid cup walls, complete right-side handle, visible oval rim and dark tea inside, front view. NO hands or sleeves, no saucer, no steam.
bottom-right (cell 7): standalone whole golden hand bell, short graspable handle at TOP, flared bell and visible clapper below, front view, intact connected silhouette, NO hand.
All eight pieces are reusable separate model components, all evenly centered in their cells; model pieces must not touch each other or cell boundaries. Genuine transparent alpha, not a checkerboard background, no opaque background, no shadow, no guides, no labels, no added text, no watermark. Avoid redesigning the character, extra props, detached body fragments, missing tail tips, clipped horns/feet or visible joint circles.

像素风在共同提示词的首段末尾补充：Match clean small pixel-sprite shading and crisp pixel edges, cute rounded proportions.

二次元风在共同提示词的首段末尾补充：Match the reference's soft detailed anime chibi shading and smooth clean line art.

## 程序中的使用

按连通轮廓的重心分配八个部件，提取整个轮廓，不在格线处截断跨格尾巴。原 PNG 不被后处理覆盖；载入时只清除透明边缘的高饱和杂色碎点。

身体固定。双臂采用 13／17 个规范像素的两段关节，肩部固定，关节连接处重叠遮盖；袖子和手只有固定比例的平移与旋转。手指贴着固定键盘小幅敲击，茶杯和两只手一起端起再放下，铃铛围绕固定握持点摆动。三个动作不调用原图位移场。

欢呼保留之前的举手姿势；跳跃恒定重力 275 规范像素每平方秒，第一跳飞行 400 ms、高度 5.5，第二跳 360 ms、高度约 4.46；两跳后有较长落地停顿。看书与抱枕休息保留。

本轮只做预览，未生成压缩包。预览由程序实际 Art.Draw 按 50 ms 采样，不加速。

developed by WodeFafa
## 当前用途更正

按用户最新反馈，独立人物／袖子／手只用于正常喝茶并修正肩部连接；工作与日程提醒已撤下这套独立手部模型，改回原完整立绘。提醒仅提取并摆动原立绘里的铃铛，不使用本图集的手或铃铛。以 ANIMATION_PREVIEW.md 最后一节为当前实现，前文保留生成记录。

## 铃铛原位置底图清理

使用 imagegen 内置编辑，透明背景，输入为运行时原始 reminder 立绘（tests/pixel-reminder-source.png 与 tests/anime-reminder-source.png），未覆盖原图集。只将生成结果用于旧铃铛遮挡区域的纹理补片，人物与手沿用原图。

像素风输出：assets/pixel-reminder-clean-v1.png；原生成文件 exec-96630e00-e6ca-48e9-94c2-6d53900df151.png。二次元输出：assets/anime-reminder-clean-v1.png；原生成文件 exec-4ab27e4f-89d6-4d20-8c8d-530111bfaaee.png。原生成文件均位于 assets/generated-sources。

实际共同提示词：

Surgical cleanup edit of this exact existing game sprite, not a redesign. Keep the full transparent square canvas and the character EXACTLY in place with EXACTLY the same scale, silhouette, pose, colors, art style, face, wings, hair, horns, feet, notebook, sleeves and ALL existing hands/fingers. Remove ONLY the small golden bell currently above/in front of the character's right-side hand (viewer right). Reconstruct the pale silver hair / sleeve and purple wing surface previously hidden behind that bell naturally, matching adjacent original pixels. The right-side existing hand itself MUST stay exactly at its original position with its original curled fingers, unchanged. Do not add a new bell, ring, arm, hand, or any prop. This is a clean character backplate for placing an animated bell later, so ALL original hand pixels must be retained. Keep existing hair ornaments. Preserve the composition and full uncut transparent sprite with unchanged coordinate placement; no padding, no zoom, no shifting.

像素风结尾：Preserve the original crisp pixel-art texture and pixel edges.
二次元结尾：Preserve the original smooth chibi anime drawing and soft shading.

运行时补片按原画对齐；只保留其背景纹理，排除生成结果中偏移的手指副本，最后覆盖回原手指像素。

## 当前二次元提醒素材用途

为修复手部模糊与只向右摆的问题，本轮未重新生成图像。二次元提醒复用已有 anime-reminder-clean-v1.png 作为高清固定人物底图，并复用 anime-activity-model-v1.png 中的竖直铃铛（第 7 号部件）；不使用该图集的人物或独立手臂。手指来自清理立绘，固定不动并置于铃铛拉环前方。图集中的铃铛现用于提醒，前文“铃铛不用此图集”的描述由本节更新；喝茶仍保留既有实现。

## 当前喝茶／敲键盘用途更正

用户要求逐帧呈现并恢复最初正常的喝茶图。当前两种风格的喝茶和工作均使用原 daily／tasks 图集的 8 张绘制姿势，独立手臂、袖子、杯子模型不再用于这两个动作。本轮没有生成或改写 PNG；已有模型图集的第 7 号铃铛仍用于用户已确认的二次元提醒动画。

## 最新完整原画播放修正

本轮无图像生成。喝茶、敲键盘继续复用原图集，但删除了上半身局部合成；每一显示帧都来自一张完整原画，整图统一等比缩放并对齐。独立手臂模型及局部位移不参与这两个动作。
