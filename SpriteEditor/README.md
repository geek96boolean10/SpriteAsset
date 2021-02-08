# SpriteEditor
Provides a tool to help sort, arrange, and save sprite images into complex sprites.

# MultiSprites
A 'sprite' is usually defined as a 2D image that moves on a computer screen. The sprite system included in SpriteAsset may represent itself as an image, but additionally includes methods and functions to swap out its current image with another variant, depending on the state of your in-game object. As such, a single sprite may actually contain several images, one of which is shown to the user at any given time. By manipulating a `SpriteAsset.Sprite`'s `Token`, a game object may alter how it is represented by selecting one of possibly many images given to it, without necessarily knowing the name or pixel bounds of the spritesheet.

# Bounds Editor
Load a bitmap (.bmp, .png) into the editor. You may then pan and zoom around the image, and demarcate the 'bounds' of any individual image. This should usually be square, and of a width/height that is a power of two, i.e. 16x16, 32x32, 64x64 etc..

Bounds may be added manually by clicking **Add Bounds**, typing in the coordinates required, then **Update Bounds**.

Bounds may be added automatically by double-clicking the top-left corner of your intended bounds, then clicking again at the bottom-right corner. 

Bounds ID values are automatic and cannot be changed in the Editor - however, if necessary, you can edit the .cmplx file in a text editor.

Removing a Bounds will also remove it from any multisprites that use it - take note of which sprites are affected before confirming deletion.

# Sprite Editor
Create new multisprites by clicking on **Add Sprite**.

Set the identifiers for the sprite by filling in its Class name and Variant name, if any. You will access this sprite via these names within your game.

Select a multisprite type to define how a token for this sprite behaves.

Add images associated with that sprite by selecting them within the Bounds list and clicking **Insert Selected**.

For each image, fill in its Assignment and Property fields as required by the multisprite type. Failure to do so properly will throw exceptions when you attempt to load the .cmplx file in your game. (I'm working on making the editor a little smarter; for now, though, the rules explained within the type description are accurate and must be followed manually.)

# Saving and Loading
There is **no autosave**. There is **no warning** when you close the app with unsaved changes.

Ensure that you are regularly saving your work by clicking **Save Progress**. The resultant file is a JSON text file with the .cmplx extension. Modify this externally at your own risk - an improper file cannot be recovered.

You may load a previously saved .cmplx file via **Load Sprite**. You may swap out the spritesheet image by clicking **Swap Image**.

To load the .cmplx file back into your game, see [SpriteAsset's README](/SpriteAsset/README.md).
