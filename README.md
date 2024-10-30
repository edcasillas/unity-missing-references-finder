<a href='https://ko-fi.com/G2G315E4XA' target='_blank'><img height='36' style='border:0px;height:36px;' src='https://storage.ko-fi.com/cdn/kofi6.png?v=6' border='0' alt='Buy Me a Coffee at ko-fi.com' /></a>

# Missing References Finder for Unity
A tool to find missing references in Unity.

Original credit to: http://www.li0rtal.com/find-missing-references-unity/

## Install ##

**Installation must be performed by project.**

1. Open the `Package Manager` in Unity (menu `Window` / `Package Manager`).
2. Press the `+` button in the top left corner of the Package Manager panel and select `Add package from git URL...`
3. When prompted, enter the URL https://github.com/edcasillas/unity-missing-references-finder.git

Alternatively, you can manually add the following line to your Packages/manifest.json file under dependencies:

    "com.ecasillas.missingrefsfinder": "https://github.com/edcasillas/unity-missing-references-finder.git"

Open Unity again; the Package Manager will run and the package will be installed.

## Update ##

1. Open the `Package Manager` in Unity (menu `Window` / `Package Manager`).
2. Look for the `Missing References Finder` package in the list of installed packages and select it.
3. Press the `Update` button.

Alternatively, you can manually remove the version lock the Package Manager creates in `Packages/manifest.json` so when it runs again it gets the newest version. The lock looks like this:

```
    "com.ecasillas.missingrefsfinder": {
      "hash": "someValue",
      "revision": "HEAD"
    }
```

## Use ##

Open the Tools menu and select `Find Missing References`, then select the context in which you want to search:
- The current scene.
- All scenes added to the build settings.
- All assets.
- Everywhere (all scenes added to the build settings + all assets).

Click on one of these options and wait for the process to finish. Missing references will be shown as errors in the console and you can click on them to jump to the corresponding game object.

## Known Issues ##
- The count of missing references shown in the dialog at the end of the process is sometimes incorrect. This doesn't affect the results shown in the console.
