# SpriteAsset

When building a 2D game in UWP, the Win2D API is useful to access hardware-accelerated graphics via Direct2D. However, trying to load sprites into your game is still a difficulty, especially if you need animated sprites, if you need to handle directional variants, or if you're using a spritesheet. I provide a library you can implement to streamline the loading process, and an editor that can build complex sprites from a spritesheet.

# Installation

You can examine and download the sideloadable app package from my DropBox here (58 MB): https://www.dropbox.com/s/sqzzkrey3hjaoxq/SpriteEditor_0.1.0.0_Sideload.zip?dl=0

After downloading the zip file, you'll need to either run the Install.ps1 PowerShell Script or manually install the certificate and app bundle. 

If you want to avoid the risks and bother of sideloading an app, I've uploaded the entire Editor's project here - simply build a Debug or Release version on your own machine to install the app. You can then run the app anytime from your start menu under Sprite Editor.

# Dependencies

These projects requires the following nuget packages:
- Newtonsoft.Json
- Win2D

These projects require Windows 10. The editor is a UWP app, so it cannot run on Windows 7 or 8.

# Readiness

This is a functional work-in-progress, hence the lack of an official Windows Store app. While I expect to be making updates and changes, the functionality within the SpriteAsset library should be solid enough that future updates will not break them. 

# Usage

See the READMEs for each project for light reading on their usages.

For more information, please see (pending Codeproject article).
