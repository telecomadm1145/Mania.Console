namespace Osu.Console.Core
{
    public interface IGameController
    {
        void Init(Game game) { }
        void Resize(int width, int height) { }
        void PushFrame(GameBuffer buffer) { }
        void Click(int x, int y, int up) { }
        void Move(int x, int y) { }
        enum WheelDirection
        {
            Horizontal,
            Vertical,
        }
        void Wheel(double delta, WheelDirection dir) { }
        void Key(KeyEvent cki) { }
        void Focus(bool focus) { }
        void Tick(TimeSpan fromRun) { }
        void Destory() { }
    }
}
