## XEmuera

XEmuera is an Android application built with [Xamarin.Forms](https://dotnet.microsoft.com/apps/xamarin). It is a port of [Emuera](https://osdn.net/projects/emuera), a text-based game engine originally for Windows.

The Emuera kernel used is based on the [私家改造版 (Custom Fix Edition) Emuera1824+v24](https://ux.getuploader.com/ninnohito/index) — a community-maintained fork of Emuera with ongoing bug fixes. The original XEmuera project was created by [Fegelein](https://github.com/Fegelein21/XEmuera), and this fork builds upon that work.

Most Windows-only features have been removed or replaced for compatibility with the Android platform. Overall runtime performance is lower than the Windows version.

This project is in an unstable development stage and may encounter various issues during use.

## Usage & Notes

- The initial startup time is long (6–8 seconds). Please wait patiently.
- Place game files in the `emuera` folder at the root of your device storage.
- The game must have both `CSV` and `ERB` folders present.
- External fonts can be added by placing `*.ttf` files in `emuera/fonts` at the storage root.
- `MS Gothic` and `Microsoft YaHei` fonts are bundled by default.
- Android 10 and above requires granting file management permission.
- Swipe from the far left edge of the screen to open the side menu.
- Configuration changes require a game reload to take effect.

## Download

https://github.com/Fegelein21/XEmuera/releases

## Known Issues

- Localization/translation is not yet fully standardized.
- A true vertical scrollbar has not been implemented due to technical limitations.

## Changelog

### Kernel Update: Emuera1824+v15 → v24

The following bug fixes from the upstream 私家改造版 have been applied:

**v24**
- Fixed: `ISNUMERIC` would throw an error for certain strings instead of returning false. Added a dedicated `NumericCheck` method.

**v23**
- Fixed: `GETNUM` and `GETNUMB` could not use `NICKNAME` or `MASTERNAME` as lookup keys.

**v22**
- Fixed: Some code paths could not correctly process `CALLNAME`.
- Fixed: `GETNUMB` was incorrectly mapped to the same handler as `GETNUM`.

**v21**
- Fixed: Debug console threw an exception when reaching 100 lines.

**v20**
- Fixed: `INPUTS` would not accept a single `@` character (it was being intercepted as a debug command prefix).

**v19**
- Fixed: Missing range-out-of-bounds handling for the 3rd and 4th arguments of `ARRAYSORT`.

**v18**
- Optimized: `CUSTOMDRAWLINE` performance improved.

**v17**
- Fixed: `GDRAWSPRITE` displayed unexpected results when enlarging a BMP-loaded image.

**v16**
- Fixed: `NOSAMES` implementation was completely wrong.
- Fixed: Using `ISASSI` or `助手` as a CSV key would cause an error; they are now silently skipped.
- Added: A warning is shown when negating `long.MinValue` (the value does not change).

## Credits

Original XEmuera project: [Fegelein](https://github.com/Fegelein21/XEmuera)

Support the original author: https://afdian.net/@fegelein21
