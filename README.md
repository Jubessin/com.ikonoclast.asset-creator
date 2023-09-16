# AssetCreator

Unity package used in the development of Ikonoclast projects, containing an editor window to facilitate the creation of assets.

![Editor Window](https://github.com/Jubessin/com.ikonoclast.asset-creator/blob/main/Documentation~/Editor_Window.png)

## Usage

Implement the `ICreatableAsset` interface on any `ScriptableObject` classes that you wish to create via the AssetCreator.

```csharp

public class CreatableAsset : ScriptableObject, ICreatableAsset
{
	// ...
}

```

For assets you wish to have only a single instance of, also implement the `ISingleInstanceAsset` interface.

```csharp

public class SingleInstanceAsset : ScriptableObject, ICreatableAsset, ISingleInstanceAsset
{
	// ...
}

```

Both the `ICreatableAsset` and `ISingleInstanceAsset` can be found in the `Ikonoclast.Common` namespace.

*Note that only non-abstract classes will be visible.*

## Dependencies

This package has dependencies. To use this package, add the following to the manifest.json file found under Assets > Packages:

* `"com.ikonoclast.common" : "https://github.com/Jubessin/com.ikonoclast.unity.common.git#3.2.0"`
