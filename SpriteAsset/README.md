# SpriteAsset
The SpriteAsset library contains several classes to help you develop the graphics for your game.

# Loading Image Data
The SpriteEditor will export .cmplx JSON files, which can be read as a string and parsed by `Newtonsoft.Json.Linq.JObject` within your game's loading phase. You can then pass this JObject into the constructor for a new `ComplexSheet` object. 

```vb.net
Public Async Function LoadFromFiles(fromDir as String) As Task(Of Boolean)
    Dim appFolder = Windows.ApplicationModel.Package.Current.InstalledLocation
    ' note that fromDir must be below the InstalledLocation by UWP limitation.
    Dim assetsFolder = Await appFolder.GetFolderAsync(fromDir)
    Dim assets = Await assetsFolder.GetFilesAsync()
    For Each file In assets
        If file.FileType <> ".cmplx" Then
            Continue For ' skip unknown file
        End If
        Dim data = Await Windows.Storage.FileIO.ReadTextAsync(file)
        ' Newtonsoft.JSON.Linq.JObject
        Dim json = JObject.Parse(data)
        ' regenerate the ComplexSheet by passing in the json object
        Dim sheet As New ComplexSheet(json)
        ...
    Next
    Return True   
End Function
```

Within the `ComplexSheet`, you'll find a field called `Sprites` that contains a list of `MultiSpriteData`. This is the raw information about a single sprite built within the editor. Convert this data into an image reference by passing both the `MultiSpriteData` and the `ComplexSheet` into the constructor for a `SpriteBundle`.

```vb.net
        Dim sheet As New ComplexSheet(json)
        For Each msdata In sheet.Sprites
            Dim bundle As New SpriteBundle(msdata, sheet)
            ...
        Next
```

Each `SpriteBundle` contains `CanvasBitmaps` that can be drawn directly to a Win2D `CanvasControl` via its `CanvasDrawingSession`. To retrieve these bitmaps, generate a `Token` from the bundle via `CreateToken()`, and apply the token via `ApplyToken()`. Tokens may be saved and reused as many times as necessary, but may only be applied to the bundle that generated it.

```vb.net
Private SomeSpriteBundle as SpriteBundle
Public Sub Canvas_Draw(sender As CanvasControl, args As CanvasDrawEventArgs) Handles Canvas.Draw
    Dim session as CanvasDrawingSession = args.DrawingSession
    Dim token as Token = SomeSpriteBundle.CreateToken()
    Dim image as CanvasBitmap = SomeSpriteBundle.ApplyToken(token)
    session.DrawImage(image)
    ...
End Sub
```
