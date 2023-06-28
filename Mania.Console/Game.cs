using Osu.Console.Core;

namespace Osu.Console
{
    public class Game
    {
        private List<IGameController> Controllers = new();
        public Game()
        {

        }
        public Game Use<T>() where T : IGameController, new()
        {
            Controllers.Add(new T());
            return this;
        }
        public T? Get<T>() where T : IGameController
        {
            return (T?)Controllers.FirstOrDefault(x => x is T);
        }
        public void Run()
        {
            Controllers.ForEach(x => x.Init(this));
        }

        public void Resize(int width, int height)
        {
            Controllers.ForEach(a => a.Resize(width, height));
        }
        public void Wheel(double delta,IGameController.WheelDirection dir)
        {
            Controllers.ForEach(a => a.Wheel(delta,dir));
        }
        public void PushFrame(GameBuffer buffer)
        {
            buffer.Clear();
            Controllers.ForEach(a => a.PushFrame(buffer));
            buffer.PushFrame();
        }
        public void Click(int x, int y, int up)
        {
            Controllers.ForEach(a => a.Click(x, y, up));
        }
        public void Move(int x, int y)
        {
            Controllers.ForEach(a => a.Move(x, y));
        }
        public void Tick(TimeSpan fromRun)
        {
            Controllers.ForEach(x => x.Tick(fromRun));
        }
        public void Stop()
        {
            Controllers.ForEach(x => x.Destory());
        }
        public void Key(KeyEvent cki)
        {
            Controllers.ForEach(x => x.Key(cki));
        }
        public void Focus(bool focus)
        {
            Controllers.ForEach(x => x.Focus(focus));
        }
    }
}
