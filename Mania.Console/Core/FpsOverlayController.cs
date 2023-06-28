namespace Osu.Console.Core
{
    class FpsOverlayController : IGameController
    {
        private Game? game;
        private TimeSpan lag;
        private TimeSpan updatecost;
        public bool Visible { get; set; } = true;
        void IGameController.Init(Game game)
        {
            this.game = game;
        }
        void IGameController.Tick(System.TimeSpan fromRun)
        {
            updatecost = fromRun - lag;
            lag = fromRun;
        }
        void IGameController.PushFrame(GameBuffer buffer)
        {
            if (Visible)
            {
                var fps = $"FPS:{game?.Get<StopWatchTickingController>().CurrentFps:F0}";
                var cost = $"{updatecost.TotalMilliseconds:F3}ms";
                buffer.DrawString(fps, (0, 180, 255), buffer.Width - Math.Max(fps.Length, cost.Length) - 3, buffer.Height - 4);
                buffer.DrawString(cost, (0, 180, 255), buffer.Width - Math.Max(fps.Length, cost.Length) - 3, buffer.Height - 3);
            }
        }
    }
}
