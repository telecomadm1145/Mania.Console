using Mania.Console;
using Mania.Console.Core;
using Osu.Console;
using Osu.Console.Core;

var game = new Game()
            .Use<ScreenController>()
            .Use<CursorController>()
            .Use<BufferController>()
            .Use<Win32ConsoleInputContoller>()
            .Use<StopWatchTickingController>()
            .Use<ManiaGame>()//.Use<LogOverlayComponent>()
            .Use<FpsOverlayController>();
game.Get<StopWatchTickingController>().TargetFps = 1000000000;
game.Run();