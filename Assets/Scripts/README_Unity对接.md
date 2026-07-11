# Unity 端对接 DigitalHuman 后端 — 部署与调试指南

> 本目录 `Assets/Scripts/` 是 Unity 端对接后端的代码集合。所有类都通过 `[AddComponentMenu("DigitalHuman/...")]` 注册到 AddComponent 菜单。

## 1. 文件地图

```
Assets/Scripts/
├── Core/
│   ├── BackendConfig.cs           ScriptableObject，httpBase/wsBase/userId/音频参数
│   ├── BackendConstants.cs        WS 事件名 / 错误码常量
│   ├── BackendEnums.cs            Emotion / Expression / AvatarAction 枚举 + wire string 映射
│   ├── DTOs.cs                    所有 REST/WS 协议类（含统一 ApiResponse）
│   └── Logger.cs                  统一前缀的 Debug.Log
├── Net/
│   ├── ApiClient.cs               UnityWebRequest 封装；解 {code,message,data}
│   ├── WebSocketChannel.cs        ClientWebSocket 封装；线程安全，主线程派发；自带 UnityMainThreadPump
│   ├── JsonExtensions.cs          JsonUtility 工具：抽出 data 子对象 JSON
│   └── MultipartBuilder.cs        手写 multipart/form-data
├── Realtime/
│   ├── RealtimeVoiceSession.cs    状态机 + 帧收发 + 事件
│   ├── MicrophoneCapture.cs       重采样到 16 kHz mono PCM16 LE；100 ms 帧
│   ├── Mp3SegmentPlayer.cs        按 tts.segment.start/end 顺序播放 MP3
│   └── WavFileWriter.cs           包装 wav 头（可选，用于 FileASR 上传）
├── Services/
│   ├── ChatService.cs             POST /chat, GET /chat/history
│   ├── ProfileService.cs          POST/GET /profile
│   ├── MemoryService.cs           GET/POST /memories, DELETE /memories/{id}
│   ├── ScheduleService.cs         CRUD /schedules
│   ├── ReminderService.cs         GET /reminders/pending, POST /reminders/{id}/ack
│   ├── ReminderChannel.cs         WS /ws/reminders/{user_id}
│   ├── CareService.cs             GET /care/next
│   ├── EmotionService.cs          POST /emotion/analyze
│   ├── TtsService.cs              POST /tts/synthesize
│   └── FileAsrUploader.cs         POST /asr/transcribe (multipart)
├── Live2D/
│   └── HiyoriDriver.cs            emotion/expression/action → Animator + CubismExpressionController
├── App/
│   ├── DigitalHumanApp.cs         顶层编排器；UI Binders 调它
│   ├── VoiceButtonBinder.cs       录音按钮
│   ├── CancelButtonBinder.cs      取消按钮
│   ├── SendButtonBinder.cs        文字发送按钮
│   ├── AckButtonBinder.cs         提醒确认按钮
│   ├── ChatHistoryView.cs         简易聊天历史 Text/TMP 渲染
│   └── StatusView.cs              多 Text/TMP 状态/错误/情绪显示
├── Editor/
│   └── DigitalHumanSceneBuilder.cs   Editor 菜单：DigitalHuman/Setup Scene 一键搭好场景
└── Debug/
    └── BackendDebugMenu.cs        Editor 菜单：日志/发送测试/打开本文件
```

---

## 2. 后端先跑通

按 `接口文档.md` 的"环境与启动"一节执行：

```powershell
# 另一终端
cd D:\...\backend
python -m uvicorn app.main:app --host 127.0.0.1 --port 8000 --workers 1
```

确认：
- `GET http://127.0.0.1:8000/health` → 200
- `GET http://127.0.0.1:8000/api/v1/ready` → `{"code":0,"data":{"ready":true,"asr":{"ready":true,...}}}`
- `http://127.0.0.1:8000/backend-test` 能开浏览器页

---

## 3. 创建 BackendConfig

1. Project 视图 → 在任意 Assets 子目录右键 → `Create / DigitalHuman / BackendConfig` → 命名 `DefaultBackendConfig`
2. 在 Inspector 中：
   - `Http Base`: `http://127.0.0.1:8000/api/v1`
   - `Ws Base`: `ws://127.0.0.1:8000/api/v1`
   - `User Id`: `1001`
   - `Voice Style`: `warm_female`
   - 其他保持默认即可

---

## 4. 场景接线（SampleScene 已存在）

打开 `Assets/Scenes/SampleScene.unity`。

### 4.1 路径 A：一键搭建（强烈推荐）

跳过下面所有手工步骤，直接：

1. Unity 顶部菜单 → `DigitalHuman` → `Setup Scene`
2. 等待进度条跑完，控制台打印 `[DH] Scene setup complete.`
3. Hierarchy 树已自动建好（详见第 8 节），按 Play 即可。

> Setup Scene 创建的所有 UI Label 默认是英文（Start Talking / Send / Cancel / Reminder / Got It 等），原因是 TMP 自带 `LiberationSans SDF` 不含 CJK 字形。要恢复中文请参考第 9 节。

### 4.2 路径 B：手工搭建 BackendRoot

创建空 GameObject `BackendRoot`，再添加以下组件。每个组件都按 AddComponent → DigitalHuman 路径找：

| AddComponent | 字段设置 |
| --- | --- |
| `DigitalHuman/App` (DigitalHumanApp) | `config` 拖入 DefaultBackendConfig |
| `DigitalHuman/Net/Api Client` (ApiClient) | （无字段，DigitalHumanApp 自动 Init） |
| `DigitalHuman/Services/Chat` | （无字段，DigitalHumanApp 自动 Inject） |
| `DigitalHuman/Services/Profile` | 同上 |
| `DigitalHuman/Services/Memory` | 同上 |
| `DigitalHuman/Services/Schedule` | 同上 |
| `DigitalHuman/Services/Reminder` | 同上 |
| `DigitalHuman/Services/Reminder Channel` | 同上 |
| `DigitalHuman/Services/Care` | 同上 |
| `DigitalHuman/Services/Emotion` | 同上 |
| `DigitalHuman/Services/Tts` | 同上 |
| `DigitalHuman/Services/File Asr Uploader` | 同上 |
| `DigitalHuman/Realtime Voice Session` | DigitalHumanApp.Awake 自动 Inject 四个 Channel/Component |

> 提示：DigitalHumanApp.Inject() 会把不在 Inspector 里拖入的 WebSocketChannel / MicrophoneCapture 用 `AddComponent` 自动创建；只需要 `chatService` 等服务类组件手工挂上即可。

### 4.3 Mp3Player

1. 在 BackendRoot 下创建一个空子物体 `Mp3Player`。
2. `Add Component` → `Audio Source`。设置 `Audio Clip=None`、`Play On Awake` 取消、`Loop` 取消、`Spatial Blend=2D`。
3. 再 `Add Component` → `DigitalHuman/Realtime/Mp3 Segment Player`。
4. 把 `Mp3Player` 拖到 `BackendRoot/DigitalHumanApp.mp3Player`。

### 4.4 HiyoriRoot（Live2D）

1. 把 `Assets/hiyori_pro/runtime/hiyori_pro_t11.prefab` 拖到场景，命名 `HiyoriRoot`。
2. 在 `HiyoriRoot` 上 `AddComponent` → `DigitalHuman/Live2D/Hiyori Driver`。会自动寻找子物体上的 `Animator` / `CubismMotionController` / `CubismExpressionController`。
3. 三个组件字段如果没自动填，把对应组件拖进来即可。
4. 把 `HiyoriRoot` 拖到 `BackendRoot/DigitalHumanApp.hiyoriDriver`。
5. **后续调整**：在 `HiyoriDriver.cs` 中编辑 `emotionClips[]` / `expressionIndices[]` / `idleStateName` 来映射具体动作/表情。

### 4.5 UI（你可以之后再做，按 Setup Scene 已自动生成）

UI Canvas 用 UGUI 或 TextMeshPro 即可。建议结构：

```
Canvas
├── StatusBar
│   ├── HealthText   (TMP_Text)
│   ├── ReadyText    (TMP_Text)
│   ├── VoiceText    (TMP_Text)
│   ├── EmotionText  (TMP_Text)
│   └── ErrorText    (TMP_Text)
├── ChatHistory      (TMP_Text，置于 ScrollView Content 内)
│   └── ChatHistoryView (Component)
├── InputField       (TMP_InputField)
├── SendButton       (Button + SendButtonBinder)
├── VoiceButton      (Button + VoiceButtonBinder)
├── CancelButton     (Button + CancelButtonBinder)
└── ReminderToast    (Panel，默认 SetActive(false))
    ├── Title (TMP_Text)
    └── AckButton (Button + AckButtonBinder)
```

### 4.6 ReminderToast

`App/DigitalHumanApp` 里的 `reminderToast` 字段：把 Panel 拖入；`reminderToastText` 字段：把 Panel 下的 Title TMP 拖入。

---

## 5. 调试清单

按这个顺序逐项验证：

1. **后端先就绪**：`GET /health` 200；`/api/v1/ready.components.asr.ready=true`。
2. **进入 Play Mode**：
   - Console 里看到 `[DH] [ws] connected ...`（reminder WS 自动连上）
   - StatusBar 里 `Health 200`、`ASR ready (cuda:0)` 等
   - `DigitalHuman / Debug / Log Current Status` 打印当前 userId/url 便于确认
3. **文字聊天**：在 InputField 输入 `Hello` → Send → ChatHistory 出现用户行 + 助手回复 + EmotionText 显示 `happy/sad/...`
4. **实时语音**：
   - 第 1 次按 `VoiceButton`：VoiceText 变成 `Voice: ready`，按钮文字变 `Stop`
   - 说话中：ChatHistory 出现 `[Me-draft] ...` 的实时 partial
   - 第 2 次按：进入 Finalizing → Responding → Playing；看到 Hiyori 切表情/动作并播放 MP3
   - 播放完按 Cancel 按钮：走 `response.cancel`，状态立刻回到 Ready
5. **提醒**：Backend 控制台手动发一条 reminder；前端 ReminderToast 弹窗；点 Got It 消失。
6. **错误码**：
   - 启动后端时先停掉 ASR → readiness 变 False → StatusView 出现 "ASR loading (...)"
   - 第二个客户端开浏览器再进 → 报 `asr_busy` → ErrorText 显示

> 排错入口：`DigitalHuman / Debug / Open Doc` 打开本文件；Console 全部以 `[DH]` 前缀方便过滤。

---

## 6. 关键技术点说明

- **WebSocket**：使用 `System.Net.WebSockets.ClientWebSocket`，BCL 自带，Unity 2022 LTS 兼容。WebSocketChannel 通过 `UnityMainThread.Pump()` 把回调调度到主线程。
- **音频**：`Microphone.Start` + `AudioClip.GetData` + 线性重采样 + 手动转 int16 LE。每 ~100ms 抛出 `byte[3200]`。
- **MP3**：`Mp3SegmentPlayer` 把每个 `tts.segment.start/end` 区间的二进制写入临时 mp3 文件，再 `UnityWebRequestMultimedia.GetAudioClip` 加载到 AudioClip，单 AudioSource 串行播放。
- **REST**：所有响应统一 `{code,message,data}`；`ApiClient` 解包时手工抽出 `data` 子 JSON 给具体 DTO。
- **Live2D**：`HiyoriDriver` 提供 Animator.Play + CubismExpressionController.CurrentExpressionIndex 两条路径。具体编号在 `expressionIndices[]` 与 `idleStateName` 中配置，后续手动调。
- **取消协议**：`response.cancel` 不会单独 ack，而是当 `error.code=response_cancelled` 时本地识别为"已取消"，回到 Ready。
- **超时**：60s 硬超时在 `RealtimeVoiceSession.OnMicFrame` 检查；超过会强制发 `utterance.end`。

---

## 7. 已知边界

- Hiyori 的具体 motion 编号没在文档中给出，`HiyoriDriver.emotionClips` 与 `HiyoriDriver.idleStateName` 需要在 Unity 中根据实际 Animator Controller 名称调整。`emotionStatePrefix = "M_"` 默认对应 `M_Neutral` / `M_Happy` 等（基于 hiyori_pro 的 motion3.json 命名约定）。
- `UnityMainThreadPump` 通过 `RuntimeInitializeOnLoadMethod` 自动挂载一个 `DontDestroyOnLoad` 的 GameObject，无需手工配置。
- 多 worker 启动 / 多人同时在线 / GPU 升级等性能事项，遵循后端说明（单 worker / 单会话）。
- Android / iOS / IL2CPP 编译需要把 `System.Net.WebSockets` 列入保留列表（Player Settings 默认保留）。
- 编译错误历史：`Action` 被重命名为 `AvatarAction` 以避免与 `System.Action` 冲突；`Logger` 在多个文件里用 `using Logger = DigitalHuman.Core.Logger;` 别名避免与 `UnityEngine.Logger` 冲突。

---

## 8. 路径 A 详解：Editor 一键搭建菜单

文件：`Assets/Scripts/Editor/DigitalHumanSceneBuilder.cs`

菜单项：`DigitalHuman / Setup Scene`

执行后会自动：

1. 在 `Assets/DigitalHumanSettings/DefaultBackendConfig.asset` 创建（已存在则复用）配置资产。
2. 在当前打开的场景中建：
   - `BackendRoot`：DigitalHumanApp + ApiClient + VoiceWsChannel + ReminderWsChannel + RealtimeVoiceSession + MicrophoneCapture + 10 个 Services
   - `Mp3Player`：AudioSource + Mp3SegmentPlayer
   - `HiyoriRoot`：`hiyori_pro_t11.prefab` 实例 + HiyoriDriver
   - `EventSystem`（独立，同名复用已有）
   - `DigitalHumanCanvas`：Canvas + CanvasScaler + GraphicRaycaster
     - `StatusBar`：5 行 TMP_Text（health/ready/voice/emotion/error）
     - `ChatPanel/ChatScroll/Viewport/Content/ChatHistory`：滚动聊天区
     - `InputBar`：TMP_InputField + InputBg + TextArea + Placeholder
     - `SendButton`：蓝色按钮 + SendButtonBinder，已串接 inputField
     - `VoiceButton`：红色按钮 + VoiceButtonBinder，按钮文字随状态切换
     - `CancelButton`：灰色按钮 + CancelButtonBinder
     - `ReminderToast`：默认隐藏的绿色 Toast，含 Title + AckButton + AckButtonBinder
3. 把以上所有 `[SerializeField]` 字段引用自动串接：
   - `app.config = cfgAsset`
   - `app.apiClient / voiceChannel / reminderSocket / realtimeSession / microphoneCapture / mp3Player / hiyoriDriver / 十个 service`
   - `app.statusView / chatHistoryView / reminderToast / reminderToastText`
4. 自动保存当前场景。

### 幂等

重复运行不会重复创建对象：相同名字的对象会被复用、新建子物体前会先清理子级。可放心反复点。

### 常见问题

| 现象 | 处理 |
| --- | --- |
| 菜单 `DigitalHuman/Setup Scene` 不存在 | 等 Unity 编译完。或右键 `Assets/Editor` 重新 Reimport |
| 报错 "Could not load ... hiyori_pro_t11.prefab" | 进入过 sample 场景后才能识别 .prefab；先开 `Assets/Live2D/Cubism/Samples/MultipleModels/MultipleModels.unity`，再回到 SampleScene 跑菜单 |
| 报错 "TMP package is missing" | Project → Window → TextMeshPro → Import TMP Essential Resources，然后重跑菜单 |
| 想自己改 UI 样式 | 在 Canvas 下手动调整 RectTransform / 颜色，脚本下次运行会先清旧子物体 |
| 想换 hiyori_free | 修改 `DigitalHumanSceneBuilder.cs` 中 `HiyoriPrefabPath` 常量即可 |

### 为什么要走 `Editor/` 目录

Unity 会自动把 `Assets/Scripts/Editor/` 与 `Assets/Scripts/**/Editor/` 下的脚本编译进 Editor assembly，因此：

1. `#if UNITY_EDITOR` 包裹整文件以双重保险；
2. `using UnityEditor` 等只能在编辑器下用；
3. 不会进最终游戏 build。

---

## 9. 后续可选：让 TMP 显示中文

Setup Scene 默认所有 Label 用英文（Start Talking / Send / Cancel / Reminder / Got It 等），因为 TMP 自带的 `LiberationSans SDF` 不含 CJK 字形。要显示中文只需给 TMP 加一个含中文的 Font Asset：

1. 从 Source Han Sans / 思源黑体 GitHub Release 下载一个 `.otf`（推荐 `SourceHanSansCN-Regular.otf`）。
2. 把字体文件复制到 `Assets/TextMesh Pro/Resources/Fonts/`（TMP_Settings 引用目录）。
3. 在 Project 选中字体 → 顶部菜单 `Window → TextMeshPro → Font Asset Creator`。
4. 勾 `Character Set = Custom Range` 并填 `19968-40869`（CJK Unified Ideographs 大致区间）；或选 `Custom Characters` 粘进需要的字列表。
5. Sampling Size 8192、SDF，Packing Mode 选 `Fast`，生成 SDF Font Asset（建议命名 `SourceHanSansCN SDF`）。
6. 在场景里把所有 TMP_Text 的 Font 字段替换成新生成资产，或在 `DigitalHumanSceneBuilder.BuildUICanvas` 里加一段：

   ```csharp
   var cjk = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
       "Assets/TextMesh Pro/Resources/Fonts & Materials/SourceHanSansCN SDF.asset");
   if (cjk != null)
   {
       chatTmp.font = cjk;
       phTmp.font = cjk;
       sendTmp.font = cjk;
       voiceTmp.font = cjk;
       cancelTmp.font = cjk;
       titleTmp.font = cjk;
       ackTmp.font = cjk;
   }
   ```

7. 同时把 `VoiceButtonBinder.cs` / `ChatHistoryView.cs` / `DigitalHumanSceneBuilder.cs` 中的英文 Label 文本换成中文即可。

最快的偷懒做法：在 ScriptableObject 模式下改 `TMP Settings → Default Font Asset` 为新的 CJK 字体资产，新建所有 TMP_Text 自动应用。

---

## 10. 当前已确认运行状态（2026-07）

- Unity 2022.3.62f2c1 编译通过
- Live2D Cubism 5-r.4.2 模型（hiyori_pro_t11）实例化正常
- 实时语音/提醒 WebSocket 在 backend 未启动时报 `Unable to connect to the remote server`，属预期，链路本身没断
- 控制台 Live2D `This MaskTexture use system: Subdivisions (Legacy)` 为 SDK 自身 info 日志，可忽略
- 13 个 Chinese unicode（取/消/连/接/中/发/送/输/入/文/字/息/略）已全部改为英文，TMP 中文字形缺失 warning 清零

后续动作建议：
1. 把后端起起来（`uvicorn app.main:app --host 127.0.0.1 --port 8000 --workers 1`）
2. 验证文字聊天 `/api/v1/chat` 返回 200
3. 用浏览器开 `/backend-test` 验证 ASR 链路
4. 切换到 Unity 按 `DigitalHuman/Setup Scene` → Play → `Log Current Status`
5. 真实录音按 VoiceButton → MP3 播放 → 数字人切表情
6. 需要中文 UI 时按第 9 节加字体
