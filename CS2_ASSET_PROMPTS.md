# CS2 形象与动画素材

工具：内置 image_gen（imagegen 技能）；透明背景 PNG。生成图未覆盖旧的走路、飞行或 osu! 素材。

## 输出

- assets/anime-cs2.png：二次元正面大头 Q 版 GPT。
- assets/pixel-cs2.png：相同朝向、手部位置的像素风 GPT。
- src/Cs2Art.cs：应用自身绘制键盘、鼠标和垫面，并通过连续网格实现头部和手臂动作。

## 二次元生成规范

参考 assets/anime-mania.png 的 GPT 身份：白银长发、浅紫龙角、紫眼、白紫服装与小龙翼。正面朝向用户、巨大的 Q 版头部约占高度 65%，小巧上半身。软光、模型玩偶风格细节。肩膀、袖子、前臂、手腕和手必须连接完整，两手向前下方伸向游戏操作区：角色自身右手位于画面左下，左手位于画面右下。只有人物透明底图，不生成键盘、鼠标、桌子、显示屏、文字或水印。四周留透明边，人物不得截断。

## 像素风生成提示

Use the anime-cs2 reference for exact front-facing pose/composition and pixel-mania for GPT identity and pixel-art style. Silver-white hair, lilac horns, violet eyes, small purple wings, white/lavender sleeves and ribbons. Big chibi head about 65 percent of height and a tiny upper body, straight front view. Both connected arms reach down and forward. Anatomical right hand on IMAGE LEFT near 22 percent width, 94 percent height; left hand on IMAGE RIGHT near 85 percent width, 94 percent height. Continuous shoulders, sleeves, elbows, wrists and hands. Pixel artwork with hard edges and clustered shading, approximately 160x144 logical detail enlarged to output. Complete silhouette with transparent padding. Character only, true alpha; no hardware, monitor, mouse, keyboard, desk, lettering, border or background.

## 运行时

手臂沿连续贴图变形，肩部位移为零、向手腕渐变。远端键位采用有限幅度的手臂趋近和下压，键帽同步高亮；不是逐根手指的三维骨骼模拟。应用使用共享顶点的三角形光栅化与预乘透明采样，避免袖子裂缝和透明边缘。画面左右角落只改变停靠位置，不镜像实体键盘。

developed by WodeFafa

## 本次调试调整

没有重新生成角色贴图，头部和手臂保持当前效果。鼠标仍在画面左侧、键盘仍在右侧，只旋转器件朝向 GPT；键盘、键帽文字与手部目标映射使用同一旋转。鼠标与手共享位移及按下状态，按键和出线转向另一端。CS2 整体显示比例减半。

## v1.2.3 握持与器件位置

角色贴图没有重新生成。鼠标与手绑定，手心一侧的后壳保持白色，手指一侧设置滚轮和左右键；不提供方向切换选项。键盘使用统一透视映射，Space 靠近角色。保持画面左侧鼠标、右侧键盘，整体显示仍为半尺寸。游戏图标保留主体轮廓、白色圆角底和最外层一道紫色圆角边框，不画内部多余方框。
