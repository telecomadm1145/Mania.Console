using System.Diagnostics;

namespace Osu.Console.Core
{
    public class StopWatchTickingController : IGameController
    {
        private Stopwatch? watch;
        private Game? game;
        private bool stopTicking = false;
        public double TargetFps { get; set; } = 30.0;
        private double curfps = 0.0;
        public double CurrentFps => curfps;
        private volatile bool isblocked = false;
        public void Block()
        {
            isblocked = true;
        }
        public void Unblock()
        {
            isblocked = false;
        }
        void IGameController.Init(Game game)
        {
            stopTicking = false;
            watch = new Stopwatch();
            watch.Start();
            this.game = game;
            (new Thread(() =>
            {
                TimeSpan lastcount = default;
                TimeSpan lastsleep = default;
                int frames = 0;
                while (!stopTicking)
                {
                    while (isblocked)
                    {
                        Thread.Sleep(0);
                    }
                    game.Tick(watch.Elapsed);
                    frames++;
                    if (watch.Elapsed - lastcount > new TimeSpan(0, 0, 1))
                    {
                        curfps = frames / (watch.Elapsed.TotalSeconds - lastcount.TotalSeconds);
                        frames = 0;
                        lastcount = watch.Elapsed;
                    }
                    while ((watch.Elapsed - lastsleep).Ticks < (long)(1 / TargetFps * 3000000))//魔法数字别乱搞（
                    {
                        Thread.Sleep(0);
                    }
                    lastsleep = watch.Elapsed;
                }
            })).Start();
        }
        void IGameController.Destory()
        {
            if (watch != null)
            {
                watch.Stop();
                watch = null;
            }
            stopTicking = true;
        }
    }
}
