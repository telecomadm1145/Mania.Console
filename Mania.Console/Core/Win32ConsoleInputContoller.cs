using Mania.Console.Core;
using System.Diagnostics;

namespace Osu.Console.Core
{
    public class Win32ConsoleInputContoller : IGameController
    {
        private Game? game;
        private bool canceled = false;
        void IGameController.Init(Game game)
        {
            NativeApi.EnableMouseInput();
            this.game = game;
            canceled = false;
            Task.Run(() =>
            {
                var rec = new NativeApi.INPUT_RECORD[1];
                bool leftclicked = false;
                bool middleclicked = false;
                bool rightclicked = false;
                while (!canceled)
                {
                    NativeApi.ReadConsoleInput(NativeApi.GetStdHandle(-10), rec, 1, out _);
                    if (!canceled)
                    {
                        switch (rec[0].EventType)
                        {
                            case NativeApi.InputRecordEventType.KEY_EVENT:
                                {
                                    var keybd = rec[0].KeyEvent;
                                    game.Key(new KeyEvent(keybd.dwControlKeyState, keybd.bKeyDown, (ConsoleKey)keybd.wVirtualKeyCode, keybd.UnicodeChar));
#if DEBUG
                                    game.Get<LogOverlayComponent>()?.PushMsg($"key:{keybd.UnicodeChar} status:{keybd.bKeyDown}");
#endif
                                    break;
                                }
                            case NativeApi.InputRecordEventType.MOUSE_EVENT:
                                {
                                    var mouse = rec[0].MouseEvent;
                                    if (mouse.dwEventFlags == NativeApi.MouseEventFlags.MOUSE_MOVED)
                                    {
                                        game.Move(mouse.dwMousePosition.X, mouse.dwMousePosition.Y);
                                    }
                                    if (mouse.dwEventFlags == NativeApi.MouseEventFlags.MOUSE_WHEELED)
                                    {
                                        game.Wheel(mouse.MouseDelta / 120, IGameController.WheelDirection.Vertical);
                                    }
                                    if (mouse.dwEventFlags == NativeApi.MouseEventFlags.MOUSE_HWHEELED)
                                    {
                                        game.Wheel(mouse.MouseDelta / 120, IGameController.WheelDirection.Horizontal);
                                    }
                                    bool leftnow = (mouse.dwButtonState & NativeApi.MouseButtonState.FROM_LEFT_1ST_BUTTON_PRESSED) == 0;
                                    if (leftnow ^ leftclicked)
                                    {
                                        game.Click(mouse.dwMousePosition.X, mouse.dwMousePosition.Y, leftnow ? 1 : 0);
                                        leftclicked = leftnow;
                                    }
                                    bool rightnow = (mouse.dwButtonState & NativeApi.MouseButtonState.FROM_LEFT_2ND_BUTTON_PRESSED) == 0;
                                    if (rightnow ^ rightclicked)
                                    {
                                        game.Click(mouse.dwMousePosition.X, mouse.dwMousePosition.Y, rightnow ? 1 : 0);
                                        rightclicked = rightnow;
                                    }
                                    bool middlenow = (mouse.dwButtonState & NativeApi.MouseButtonState.RIGHTMOST_BUTTON_PRESSED) == 0;
                                    if (middlenow ^ middleclicked)
                                    {
                                        game.Click(mouse.dwMousePosition.X, mouse.dwMousePosition.Y, middlenow ? 1 : 0);
                                        middleclicked = middlenow;
                                    }
                                    break;
                                }
                            case NativeApi.InputRecordEventType.WINDOW_BUFFER_SIZE_EVENT:
                                {
                                    var bsize = rec[0].WindowBufferSizeEvent;
                                    game.Resize(bsize.dwSize.X, bsize.dwSize.Y);
                                    break;
                                }
                            case NativeApi.InputRecordEventType.FOCUS_EVENT:
                                {
                                    var focus = rec[0].FocusEvent;
                                    game.Focus(focus.bSetFocus == 0 ? false : true);
                                    break;
                                }
                            default:
                                {
                                    break;
                                }
                        }
                    }
                }
            });
        }
        void IGameController.Destory()
        {
            canceled = true;
        }
    }
}
