# TakeMePop

[ English | [日本語](https://github.com/hetima/TakeMePop/blob/main/README-ja.md) ]

A application for Windows that pops up a floating window to serve as a relay point for drag-and-drop operations. It also provides clipboard history and temporary text editing features.

When launched, it stays resident in the taskbar. To quit, right-click the taskbar icon and choose Exit. You can customize its behavior from Setting.

## Installation

Download it from the [releases page](https://github.com/hetima/TakeMePop/releases) or install it via [Scoop](https://scoop.sh/).

```sh
# Install
scoop bucket add hetima https://github.com/hetima/scoop-bucket
scoop install hetima/TakeMePop

# Launch
TakeMePop

# Update
scoop update hetima/TakeMePop
```

The .NET 10 Desktop Runtime is required to run this application. If it is not installed, install it from [here](https://dotnet.microsoft.com/download/dotnet/10.0) or run the following command:

```
winget install Microsoft.DotNet.DesktopRuntime.10
```

## Drop Panel

A small window for temporarily holding files or text.

- Shown via a global shortcut, even while dragging.
- Press Ctrl+C twice to show a panel holding the copied files. You don't even need to start dragging.

You can drop directly onto the panel, which replaces its contents.
Once a drag-and-drop from the panel to another location completes, the panel closes (it may not close for applications such as Explorer where drop detection is ambiguous).

Press the round button at the top-left of the panel to toggle pinning. While pinned, the panel no longer closes automatically. Normally a single panel is reused, but when pinned it is exempt from that rule, allowing you to show multiple panels.


## Quick Text Edit

A small window for temporarily holding and editing text. It's handy when you want to make small edits while typing into a chat etc.

- Shown via a global shortcut.
- Press Ctrl+C twice to show a panel holding the copied text.


## Clipboard History

A small window that shows the 4–10 most recent clipboard entries. Hold the modifier key set in the shortcut and press the trigger key to show and select an entry; releasing the modifier key pastes it. It can also be configured to behave in the conventional way: show via a shortcut, then click to select (copying to the clipboard).


## Drop Guard

This feature reduces mistakes such as "I meant to click but it turned into a drag, and I dropped the item into the adjacent folder." When a drag begins, it briefly shows a transparent window only at the mouse pointer's location. This window absorbs the drop and makes it as if nothing happened. In Preferences you can fine-tune the window size, how many pixels of movement triggers it, how many seconds until it disappears.


### Info

- Supported OS: Windows 10?/11
- Written in C#, .NET10, WPF


## License

MIT License
