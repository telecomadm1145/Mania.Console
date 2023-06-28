using Osu.Console.Core.Screen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Osu.Console.Core
{
    public class ScreenController : IGameController
    {
        public IScreen? CurrentScreen { get; private set; } = null;
        public void Navigate<T>(T? screen) where T : IScreen
        {
            if (CurrentScreen != null)
            {
                CurrentScreen.NavigateTo(screen);
            }
            if (screen != null)
                screen.NavigateFrom(CurrentScreen);
            CurrentScreen = screen;
            if (gameref != null)
                CurrentScreen?.Init(gameref);
        }
        Game? gameref;
        void IGameController.Init(Game game)
        {
            gameref = game;
        }
        void IGameController.Resize(int width, int height)
        {
            CurrentScreen?.Resize(width, height);
        }
        void IGameController.PushFrame(GameBuffer buffer)
        {
            CurrentScreen?.PushFrame(buffer);
        }
        void IGameController.Click(int x, int y, int up)
        {
            CurrentScreen?.Click(x, y, up);
        }
        void IGameController.Move(int x, int y)
        {
            CurrentScreen?.Move(x, y);
        }
        void IGameController.Key(KeyEvent cki)
        {
            CurrentScreen?.Key(cki);
        }
        void IGameController.Tick(TimeSpan fromRun)
        {
            CurrentScreen?.Tick(fromRun);
        }
        void IGameController.Wheel(double delta, Osu.Console.Core.IGameController.WheelDirection dir)
        {
            CurrentScreen?.Wheel(delta, dir);
        }
        void IGameController.Destory()
        {
            CurrentScreen = null;
        }
    }
}
