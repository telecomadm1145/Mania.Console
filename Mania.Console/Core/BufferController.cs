namespace Osu.Console.Core
{
    public class BufferController : IGameController
    {
        private Game? game;
        private GameBuffer? buffer;
        public GameBuffer? CurrentBuffer => buffer;
        void IGameController.Init(Game game)
        {
            this.game = game;
            buffer = new();
            buffer.InitConsole();
            buffer.ResizeBuffer();
        }
        void IGameController.Tick(TimeSpan fromRun)
        {
            if (buffer != null)
                game!.PushFrame(buffer);
        }
        void IGameController.Resize(int width, int height)
        {
            if (buffer == null)
                return;
            if (width <= 0 || height <= 0)
                throw new ArgumentOutOfRangeException(nameof(width));
            game!.Get<StopWatchTickingController>().Block();
            Thread.Sleep(10);
            buffer.Clear();
            buffer.PushFrame();
            if (buffer != null)
            {
                buffer.Height = System.Console.WindowHeight;
                buffer.Width = System.Console.WindowWidth;
                buffer.ResizeBuffer();
            }
            game!.Get<StopWatchTickingController>().Unblock();
        }
    }
}
