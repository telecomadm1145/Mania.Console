namespace Osu.Console.Core
{
    public class CursorController : IGameController
    {
        internal (int x, int y) mousepos;
        public (int x, int y) MousePosition => mousepos;
        public bool Visible { get; set; } = true;
        private bool clicked;
        void IGameController.PushFrame(GameBuffer buffer)
        {
            if (!Visible)
                return;
            buffer.TrySetPixel((clicked ? '+' : 'x', ((255, 255, 255), (0, 0, 0))), mousepos.x, mousepos.y);
        }
        void IGameController.Click(int x, int y, int up)
        {
            clicked = up == 0;
        }
        void IGameController.Move(int x, int y)
        {
            mousepos.x = x;
            mousepos.y = y;
        }
    }
}
