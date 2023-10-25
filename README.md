# MArchiveBatchTool

This is the latest version of [MArchiveBatchTool](https://gitlab.com/modmyclassic/sega-mega-drive-mini/marchive-batch-tool)
that's still a command-line tool and not just a library, with two fixes for files larger than 4GB.

## Prebuilt binaries

Binaries are built via GitHub Actions.

Windows and Linux builds should work as is but have not yet been tested.

macOS executables are not "notarized" (code signed), but in my testing it still works.

If macOS refuses to run MArchiveBatchTool, try:

```
xattr -rd com.apple.quarantine MArchiveBatchTool *.dylib *.dll
```
