<center>

<img src="docs/images/EyeGuard.png" height="256">
<h1>EyeGuard</h1>
</center>

![A black window with a 20-second timer and a skip break button](docs/images/RestWindow.png)

## How it works
EyeGuard will run at startup in the background and send you a notification every 20 minutes
(according to the
<a href="https://www.medicalnewstoday.com/articles/321536#supporting-evidence">20 20 20 rule</a>).
It will show a timer for how long you should redirect your eyes, and disappear after it ends.

If you are in focus mode, playing a game, or your app is taking up the full monitor it rests on, then
EyeGuard will skip the break to avoid interrupting you.

## Built with
EyeGuard is built with the Windows App SDK, WinUI 3, and P/Invoke. 
The latest version is inspired by 
<a href="https://github.com/slgobinath/SafeEyes">SafeEyes</a>, by slgobinath.
It is distribued with MSIX packaging, and built in Visual Studio.

If you'd like to contribute, set up the Windows App SDK through Visual Studio,
and have at it! If you encounter any issues, feel free to reach out via email or
GitHub issues/discussions.