<p align="center">
  <a href="https://sentry.io" target="_blank" align="center">
    <img src="https://sentry-brand.storage.googleapis.com/sentry-logo-black.png" width="280">
  </a>
  <br />
</p>

Sentry (lite) SDK for Unity
===========

This is the stable Sentry SDK for Unity. It's running in production in many games and sends millions of events every month to [sentry.io](sentry.io).
If you are used to other Sentry SDKs, you might find that the API here is smaller. This is by design, and this SDK is lightweight and compile with your game.
It supports any platform that you can target with Unity.

### Installation

#### Through the package manager

![Install git package screenshot](./Documentation~/install-git-package.png)

Open the package manager, click the + icon, and add git url.

```
https://github.com/getsentry/sentry-unity-lite.git#1.0.2
```

#### Through unitypackage

The [Releases page](https://github.com/getsentry/sentry-unity-lite/releases) include a `.unitypackage` which you can simply drag and drop into your project.

### Usage

In order to make Sentry work, you need to add `SentrySdk` component to any
`GameObject` that is in the first loaded scene of the game.

You can also add it programatically. There can only be one `SentrySdk`
in your whole project. To add it programatically do:

```C#
var sentry = gameObject.AddComponent<SentrySdk>();
sentry.Dsn = "__YOUR_DSN__"; // get it on sentry.io when you create a project, or on project settings.
```

The SDK needs to know which project within Sentry your errors should go to. That's defined via the DSN.
DSN is the only obligatory parameter on `SentrySdk` object.

This is enough to capture automatic traceback events from the game. They will
be sent to your DSN and you can find them at [sentry.io](sentry.io)

`SentrySdk` is the main component that you have to use in your own project.

### Example

The package includes a Demo scene. `SentryTest` is a component that handles
button presses to crash or fail assert.

### API

The basic API is automatic collection of test failures, so it should mostly
run headless. There are two important APIs that are worth considering.

* collecting breadcrumbs

  ```C#
  SentrySdk.AddBreadcrumb(string)
  ```

  will collect a breadcrumb.

* sending messages

  ```C#
  SentrySdk.CaptureMessage(string)
  ```

  would send a message to Sentry.

### Unity version

The lowest required version is Unity 5.6.
Previous versions might work but were not tested and will not be supported.


### Native Crash Support

Sentry is [working on a Unity SDK](https://github.com/getsentry/sentry-unity) based on the .NET SDK which includes offline caching and native crashes.
Previews of that package are available. If you'd like to get involved in the SDK development, you can [join Sentry's Discord server and say hi on the `#unity` channel](https://discord.gg/UmjjsgRAFa).

## Resources

* [![Documentation](https://img.shields.io/badge/documentation-sentry.io-green.svg)](https://docs.sentry.io/platforms/dotnet/)
* [![Forum](https://img.shields.io/badge/forum-sentry-green.svg)](https://forum.sentry.io/c/sdks)
* [![Discord Chat](https://img.shields.io/discord/621778831602221064?logo=discord&logoColor=ffffff&color=7389D8)](https://discord.gg/PXa5Apfe7K)  
* [![Stack Overflow](https://img.shields.io/badge/stack%20overflow-sentry-green.svg)](http://stackoverflow.com/questions/tagged/sentry)
* [![Twitter Follow](https://img.shields.io/twitter/follow/getsentry?label=getsentry&style=social)](https://twitter.com/intent/follow?screen_name=getsentry)
