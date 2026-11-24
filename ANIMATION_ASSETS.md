# 最终动画素材

本项目全部角色素材使用内置 ImageGen 生成，参考图为项目原有 `assets/anime.png` 和 `assets/pixel.png`。没有使用 API 密钥或第三方下载精灵。

本次稳定修正：原始 PNG 保持不变，运行时使用固定人物底图，头部配准只用于局部手臂／道具参考，整个人物不再逐帧替换。头部、双脚、下部躯干为固定像素；伸展区域同时覆盖手臂原位置与目标位置，避免保留旧手。阅读使用固定持书姿势，书页由独立图层从右侧跨书脊翻至左侧并放平。

新增日常与任务动作：`pixel-daily-8.png`、`anime-daily-8.png`、`pixel-tasks-8.png`、`anime-tasks-8.png`。每张八列、四行，每行一个动作的八张连续原画；共 128 帧。使用内置 ImageGen 参考既有形象生成，最终提示词见 ACTIVITY_ASSET_PROMPTS.md。运行时根据真实透明行间距切出各行，移除相邻碎片后以整行共用边界加载，不能逐帧拉伸或数学等分裁掉脚部。动作有各自帧间停留时间，看书先停留再翻页；随机调度仅包含日常四组，任务四组按实际状态触发，番茄钟结束使用休息动作。GIF 来自实际 Art.Draw 渲染，再编码为动画文件。

当前运行优先使用：

- `anime-rig.png` / `pixel-rig.png`：固定身体、前后手臂、大腿、小腿及靴子的分层图集。程序按关节连接、使用连续步态，不再切换整张走路立绘。双腿相差半个周期，手臂反向摆动。腿部以素材原始比例为基础加粗；鞋与靴筒在踝部拆分绘制，支撑脚鞋底保持水平，靴筒随膝盖弯曲；运行时剔除关节圆圈和靴子上方多余皮肤段，连接点藏在裙摆后。大腿与小腿骨长恒定，避免走路时伸缩。原始 PNG 保留不变。
- `anime-wings.png` / `pixel-wings.png`：两片独立翅膀，飞行时围绕根部连续扇动。
- `anime-pickup.png` / `pixel-pickup.png`：完整横向揪起图片，整图加载，轻微摇摆。旋转时按旋转后的包围范围缩放，避免截断。
- `anime-lift.png` / `pixel-lift.png`：仅保留放下后回正、落脚、站稳的过渡。旧四帧揪起已由独立图片替代。
- `anime-walk.png` / `pixel-walk.png`：早期八帧素材，只作为缺少分层资源的构建回退；正常发行版使用分层动画。

图片生成提示词主体如下，每种风格分别附加 `Smooth Japanese chibi anime cel-shaded illustration` 或 `Crisp authentic 16-bit pixel art, square pixels and limited palette`。

## 分层步态

Production 2D skeletal animation PARTS sprite atlas, not animation frames. Use the reference for exact character identity/clothes. EXACT 4 columns x2 rows, 8 isolated cutout parts on TRUE transparent alpha background. One part centered per cell with large 20% transparent gutters; no grid, text or labels. All views strictly SIDE PROFILE FACING RIGHT. White-haired purple-eyed small dragon girl, pale little horns, white/lilac dress with plum bow, white boots with violet accents, tail and wings. The software will animate the limbs independently so the torso must stay fixed. Cell1 top-left: CORE BODY from horns and head to skirt hem, long hair, tail and little wings attached; NO arms, NO hands, NO legs, NO feet anywhere, both shoulders terminate neatly at sleeve opening, skirt bottom empty, hair and tail end about skirt hem so they will not cover animated legs. Cell2: complete NEAR arm with white bell sleeve and hand, a single relaxed arm pointing STRAIGHT DOWN, shoulder attachment at top, hand at bottom. Cell3: matching FAR arm, slightly lavender shaded, relaxed straight down. Cell4: NEAR upper-leg/thigh piece only, pale thigh from hip joint top to knee joint bottom, straight vertical, rounded joint caps; no boot or foot. Cell5 bottom-left: matching FAR thigh, lavender shaded, straight vertical. Cell6: NEAR lower-leg and white boot, knee joint at TOP, shaft straight vertical, sole horizontal with toe facing RIGHT, lavender bow detail. Cell7: FAR lower-leg/boot matching cell6 but softly lavender shaded, toe RIGHT. Cell8: full assembled reference character standing side profile facing right, same core body and all the same limb parts. Preserve the familiar character. No repeated full characters except cell8. Transparent cutout parts must never touch cell edges. These parts must resemble a coherent game animation rig, crisp edges, no gradients or glows outside silhouettes.

## 翅膀

Production 2D animation wing PARTS atlas for the white-haired lavender dragon girl reference. EXACTLY TWO COLUMNS and ONE ROW, two isolated wings, true transparent alpha background. Left cell: her NEAR lavender dragon/bat wing fully spread, shoulder ROOT at far RIGHT near lower corner, three slender violet wing bones spreading toward LEFT and UP, elegant pale lavender membrane scalloped along lower edge, small pale horn at tip. Right cell: matching FAR wing in slightly darker lavender shade, same orientation and root at RIGHT, fully spread. Only these two separate wings, NO character, body, limbs, hands, tail, labels, text, grid, sparkle, backdrop or shadows. The software rotates/scales wings about their RIGHT-side root for smooth flight animation. Keep each wing inside central 75% of its cell with transparent padding. Match reference colors and line style exactly. Clean cutout silhouette, no background gradient.

## 完整揪起图

Create ONE SINGLE full-body sprite of the EXACT white-haired purple-eyed dragon girl from the reference, same identity, clothes, horns, wings and tail. NOT a sprite sheet, NOT a grid, only ONE character. Pose: gently lifted at the middle of her WAIST/back by an invisible cursor. Body lies HORIZONTAL, belly facing DOWN, waist is the highest point at center, head droops down toward LEFT, BOTH arms hang down on left, BOTH knees and boots hang down on right. Hair and dragon tail naturally droop; tail fully visible to right. Cute slightly surprised face. No hand/cursor drawn. No distress or injury, modest intact white/lilac dress with plum bow, white boots, pale horns and little wings. Full head, all hair ends, all four limbs and ENTIRE tail must be visible with 20% empty transparent border on EVERY side. Character fits comfortably in central60% of canvas. Actual transparent background, no shadows, text, particles, grid, floor or background color. Keep limbs clear and anatomically correct. Horizontal floppy plush-toy-like waist-held pose, not standing and not flying.

阅读位置修正（v1.2.4）：书页平面按原图中真实书脊、左右上缘、可见页块分别校准，原图不变；每翻三页复用原 anime.png／pixel.png 的第 3 格作为持书思考，停留 6 秒后恢复翻页。原姿势只在阶段切换时展示，使用相同脚底基线和水平头部锚点，不逐帧重定位。reading-aligned.gif 覆盖整个循环。

## v1.2.5 书页与姿势衔接

书页从最上层纸张略上方开始，固定在上缘凹槽，内沿连续连到封面书脊。保留旧思考原画，使用面部、书本／手和下身的对应网格形变与预乘透明合成，进入／退出各 700 ms、15 个画面，避免直接淡化出现双脸。原动作稳定保持 6000 ms；缓存随延后动作预热生成。未生成新整图，其他七组动作沿用已固定人物的八帧版本。预览由最终 Art.Draw 20 Hz 采样。
