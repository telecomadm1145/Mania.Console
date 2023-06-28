namespace Osu.Console.Core
{
    public struct KeyEvent
    {
        internal KeyEvent(NativeApi.ControlKeyState cks, bool down, ConsoleKey key, char chr)
        {
            KeyState = cks;
            Pressed = down;
            Key = key;
            UnicodeChar = chr;
        }
        public bool AltDown => LeftAltDown || RightAltDown;
        public bool LeftAltDown => (KeyState & NativeApi.ControlKeyState.LEFT_ALT_PRESSED) != 0;
        public bool RightAltDown => (KeyState & NativeApi.ControlKeyState.RIGHT_ALT_PRESSED) != 0;
        public bool CtrlDown => LeftCtrlDown || RightCtrlDown;
        public bool LeftCtrlDown => (KeyState & NativeApi.ControlKeyState.LEFT_CTRL_PRESSED) != 0;
        public bool RightCtrlDown => (KeyState & NativeApi.ControlKeyState.RIGHT_CTRL_PRESSED) != 0;
        public bool NumLock => (KeyState & NativeApi.ControlKeyState.NUMLOCK_ON) != 0;
        public bool CapsLock => (KeyState & NativeApi.ControlKeyState.CAPSLOCK_ON) != 0;
        public bool ScrollLock => (KeyState & NativeApi.ControlKeyState.SCROLLLOCK_ON) != 0;
        public char UnicodeChar { get; set; }
        public ConsoleKey Key { get; set; }
        public bool Pressed { get; set; }
        internal NativeApi.ControlKeyState KeyState { get; set; }
    }
}
