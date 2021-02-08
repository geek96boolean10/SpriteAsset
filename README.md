# SpriteAsset

When building a 2D game in UWP, the Win2D API is useful to access hardware-accelerated graphics via Direct2D. However, trying to load sprites into your game is still a difficulty, especially if you need animated sprites, if you need to handle directional variants, or if you're using a spritesheet. I provide a library you can implement to streamline the loading process, and an editor that can build complex sprites from a spritesheet.

# Installation

In order to avoid certificates, signing, and other publishing bothers, I've opted to upload the entire Editor's project here. Simply build a Debug or Release version on your own machine to install the app. You can then run the app anytime from your start menu under Sprite Editor.

# Dependencies

These projects requires the following nuget packages:
- Newtonsoft.Json
- Win2D

These projects require Windows 10. The editor is a UWP app, so it cannot run on Windows 7 or 8.

# Readiness

This is a functional work-in-progress, hence the lack of an official Windows Store app. While I expect to be making updates and changes, the functionality within the SpriteAsset library should be solid enough that future updates will not break them. 

# Usage

Pending Codeproject article.
