# Missing References Finder for Unity
A tool to find missing references in Unity.

Original credit to: http://www.li0rtal.com/find-missing-references-unity/

## Install ##

**Installation must be performed by project.**

Add the following line to your Packages/manifest.json file under dependencies:

    "com.ecasillas.missingrefsfinder": "https://github.com/edcasillas/unity-missing-references-finder.git"
    
Open Unity again; the Package Manager will run and the package will be installed.

## Update ##

To ensure you have the latest version of the package, remove the version lock the Package Manager creates in Packages/manifest.json. The lock looks like this:

```
    "com.ecasillas.missingrefsfinder": {
      "hash": "someValue",
      "revision": "HEAD"
    }
```

## Use ##

Open the Tools menu and select "Find Missing References", then select the context in which you want to search:
- The current scene
- All assets
- All scenes

Click on one of these options and wait for the process to finish. Missing references will be shown as errors in the console and you can click on them to jump to the corresponding game object.

