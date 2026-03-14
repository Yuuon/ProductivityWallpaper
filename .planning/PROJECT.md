# ProductivityWallpaper

**Project Type:** Desktop Application  
**Status:** Brownfield (Active Development)  
**Created:** 2026-03-14

## Overview

ProductivityWallpaper is a Windows desktop application that brings interactive "看板娘" (virtual companion) experiences to the desktop through dynamic wallpaper layers. Built with WPF and .NET 8, it mounts video content behind desktop icons while capturing user interactions through a transparent overlay layer.

## Core Value Proposition

Transform static wallpapers into interactive companions that respond to clicks, hovers, and gestures while maintaining full desktop functionality.

## Technology Foundation

- **Framework:** WPF (.NET 8)
- **Media Engine:** LibVLCSharp + VideoLAN.LibVLC.Windows
- **Pattern:** MVVM with CommunityToolkit.Mvvm
- **Platform:** Windows 10/11 (P/Invoke to User32.dll)
- **Architecture:** Three-layer window system with WorkerW injection

## Existing Capabilities

### Implemented Features
1. **Three-Layer Window Architecture**
   - Idle video layer (bottom) - Looping background content
   - Action video layer (middle) - Triggered animations
   - Interactive UI layer (top) - Transparent hotspot overlay

2. **Multi-Hotspot Click System**
   - Head, Body, Special trigger zones
   - Independent action videos per zone
   - Audio playback support

3. **Hover Detection**
   - Configurable delay (default 500ms)
   - Bubble UI with positioning
   - Text display per trigger

4. **Sweep Gesture Detection**
   - Speed calculation (default threshold: 3000 px/s)
   - 5-position history tracking
   - Cooldown mechanism (500ms)

5. **Media Library Management**
   - Folder browsing with thumbnails
   - MP4/WebM/MKV support
   - JSON-based metadata caching

6. **Configuration System**
   - Interactive config per wallpaper (`config.wallpaper`)
   - User preferences (`user_config.json`)
   - Localization (en-US, zh-CN)

## Target Users

- Desktop customization enthusiasts
- Anime/virtual companion fans
- Windows power users seeking interactive desktops
- Content creators building wallpaper modules

## Development Context

This is an **existing codebase** with active functionality. The GSD workflow will focus on:
1. **Enhancement Phase:** Adding system awareness features
2. **Quality Phase:** Addressing technical concerns and adding tests
3. **Extension Phase:** Media format support and AI integration

## Key Constraints

- Windows-only (WorkerW dependency)
- Video transparency not supported (HwndHost limitation)
- Requires global mouse hooks
- Anti-flicker initialization sequence required

## Success Metrics

- Smooth window initialization without flickering
- Responsive interaction detection (<16ms latency)
- Memory stable during extended use
- Crash-free operation across Windows versions
