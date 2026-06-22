IDESK
=====

Windows 桌面生产力工具集 — 待办、笔记、日程、习惯追踪、翻译、AI 助手，全在一个浮动面板里。

## 功能

| 模块          | 说明                                                   |
| ----------- | ---------------------------------------------------- |
| **AI 助手**   | 接入 OpenAI / Anthropic API 的对话助手，支持多轮工具调用（增删待办、查询日程等） |
| **待办**      | 多分组待办列表，支持截止日期、完成状态切换                                |
| **笔记**      | 多实例文本笔记，自动保存                                         |
| **日程**      | 日历视图日程管理，支持今日卡片                                      |
| **习惯**      | 每日打卡习惯追踪，周视图                                         |
| **计划**      | 每日计划管理，支持 AI 规划助手                                    |
| **计划 Lite** | 轻量版计划，无 AI 功能，数据独立                                   |
| **翻译**      | 基于 LLM 的翻译，支持单词/短语/文段，中英互译                           |
| **截图识别**    | 截屏 + OCR 识别，直接发送到 Chat 或翻译窗口                         |
| **语音输入**    | 基于 Windows System.Speech 的中文语音识别                     |
| **书签系统**    | 窗口拖拽到屏幕边缘自动吸附为书签，50+ 预设样式                            |
| **多主题**     | 深色/浅色主题切换                                            |

## 快速开始

### 系统要求

- Windows 10 19041+（Windows 10 20H1 及以上）
- [.NET 10 Runtime](https://dotnet.microsoft.com/download/dotnet/10.0)

### 下载

从 [Releases](https://github.com/kaiiiz/IDESK/releases) 下载最新版本的 `IDESK.exe`，直接运行即可。

### 从源码构建

需要先安装 [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)。

```bash
git clone https://github.com/kaiiiz/IDESK.git
cd IDESK
dotnet publish src/IDESK.Host/IDESK.Host.csproj -c Release -o publish --self-contained true -p:RuntimeIdentifier=win-x64 -p:PublishSingleFile=true
```

构建产物在 `publish/IDESK.exe`，可直接运行。

### 配置 API

首次使用 AI 功能前，需要在控制台设置 → LLM 配置中填写 API URL 和 Key。

**默认快捷键**

| 快捷键           | 功能           |
| ------------- | ------------ |
| `Alt+W`       | 打开控制台        |
| `Alt+S`       | 显示/隐藏所有组件    |
| `Alt+Space`   | 打开 Chat 临时窗口 |
| `Alt+C`       | 打开翻译临时窗口     |
| `Alt+Shift+S` | 截图识别 → Chat  |
| `Alt+Shift+C` | 截图识别 → 翻译    |
| `Alt+F3`      | 调试窗口         |
| `Alt+X`       | 组件置顶闪现       |
| `Alt+V`       | 全部最小化到书签     |

所有快捷键可在控制台设置 → 按键绑定中自定义。

## 技术栈

- **.NET 10** — WPF 桌面应用框架
- **Entity Framework Core** — SQLite 本地存储
- **Markdig** — Markdown 渲染
- **NAudio** — 音频录制（已废弃，改用 System.Speech）
- **System.Speech** — Windows 语音识别

## 项目结构

```
src/
├── IDESK.Core/          # 核心库：窗口基类、主题、图标、工具
├── IDESK.Host/          # 启动项目：DI 注册、快捷键、进程单例
├── IDESK.Console/       # 控制台主窗口：设置、快捷键绑定、组件管理
├── IDESK.Widgets.Todo/  # 待办组件
├── IDESK.Widgets.Notes/ # 笔记组件
├── IDESK.Widgets.Plan/  # 计划组件 + Plan Lite
├── IDESK.Widgets.Habit/ # 习惯组件
├── IDESK.Widgets.Schedule/    # 日程组件
├── IDESK.Widgets.Translate/   # 翻译组件（桌面）
├── IDESK.Transient.Chat/      # Chat 临时窗口
├── IDESK.Transient.Translate/ # 翻译临时窗口
```

## 特殊说明

本项目目前属于早期版本，有很多内容尚不成熟，正式版会进行更系统的需求分析，并对代码进行大规模重构，并且更换框架

## License

MIT
