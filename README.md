# Donaldows Vista SP1 Hotfix 091016 for C#

An unofficial C# / .NET port of the classic joke software **Donaldows** for Windows.

This project aims to reproduce the original Donaldows experience on modern Windows using **C#/.NET and WinUI 3**.

> **日本語版:** [README.ja.md](README.ja.md)

## About Donaldows

Donaldows was a joke software project created around 2009. Its original implementation was written in **HSP (Hot Soup Processor)**.

The original source code contains the following statement from its author:

> 「改造しても構いませんが（例：エア本や兄貴のOSにするとか）」

The author explicitly stated that modification of the source code was permitted and even mentioned creating derivative versions as an example.

However, the original source code does **not appear to contain a formal open-source license** such as the MIT License or GNU GPL. Therefore, this project does not assume that the original work was released under a standard open-source license.

## About this port

This project is an **independent reimplementation / port** of Donaldows.

The original HSP source code has not been directly copied into this project. The C# implementation was written separately for .NET and WinUI 3.

The behavior and appearance of the original Donaldows are intentionally reproduced as closely as practical, with minor changes and bug fixes where necessary for the modern Windows environment.

## Original assets

Most files under `Assets/` originate from the original Donaldows software.

These original assets are **not covered by the license of the newly written C# source code** in this repository.

Please do not assume that the MIT License in this repository grants additional rights to the original Donaldows assets.

The following assets are exceptions and originate from the WinUI / Windows application project template rather than from the original Donaldows project:

- `Assets/LockScreenLogo.scale-200.png`
- Other template-generated assets, where applicable

See the relevant Microsoft documentation and project templates for the origin of these files.

## License

### New source code

Unless otherwise stated, the source code newly written for this port is licensed under the **MIT License**.

Copyright (c) 2026 yumasansansan

### Original Donaldows material

Original Donaldows assets and other material derived from the original work are **not included under the MIT License** unless explicitly stated otherwise.

The original work remains subject to its original copyright and other applicable rights.

The original author is believed to have passed away in November 2018.

## Disclaimer

This project is an unofficial fan-made port and is not affiliated with, endorsed by, or sponsored by the original author or any organization associated with Donaldows.

Donaldows was originally created as joke software. This project is provided for historical, educational, and entertainment purposes.

The original software and its assets may contain references, names, images, sounds, or other material whose rights belong to their respective copyright holders.

## Requirements

- Windows 11
- .NET 10
- Windows App SDK / WinUI 3

See the project files for the exact framework and SDK versions required to build the application.

## Building

Open:

```text
Donaldows_Vista_SP1_hotfix091016_Csharp.slnx
```

in a compatible version of Visual Studio and build the project.

## Project status

This project is primarily intended as a preservation / porting project.

Compatibility with the original Donaldows is the main goal, rather than introducing major new features.