using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Osu.Console.Core.Screen
{
    public interface IScreen
    {
        void Init(Game game) { }
        void Resize(int width, int height) { }
        void PushFrame(GameBuffer buffer) { }
        void Click(int x, int y, int up) { }
        void Wheel(double delta,IGameController.WheelDirection dir) { }
        void Move(int x, int y) { }
        void Key(KeyEvent cki) { }
        void Tick(TimeSpan fromRun) { }
        bool NavigateFrom(IScreen? from) { return true; }
        bool NavigateTo(IScreen? to) { return true; }
    }
}
