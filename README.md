# doublestars
Publish double stars measurements as markdown files

## Requirements

* [.NET 6 or later](https://dotnet.microsoft.com/en-us/)
* [libvips](https://github.com/libvips/libvips) `brew install vips`
* [netvips](https://github.com/kleisauke/net-vips)

## Installation

The macOS native binaries available from [netvips](https://github.com/kleisauke/net-vips) **cannot** be used since they are built [without FITS support](https://github.com/kleisauke/libvips-packaging/blob/19ab4e00488056bc4f6276c6a65a66146dc6da48/build/lin.sh#L506).

The easiest alternative is to install [libvips](https://github.com/libvips/libvips) using homebrew and make sure the `libvips.42.dylib` native library is available in your `PATH` or add `/opt/homebrew/Cellar/vips/8.12.1/lib` to your `DYLD_FALLBACK_LIBRARY_PATH`.
