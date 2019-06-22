using System;
using DivaHook.Emulator.Input;

namespace DivaHook.Emulator.Config
{
    public struct ControlBindingAccessor
    {
        public Func<ControlBinding> Getter;
        public Action<ControlBinding> Setter;

        public ControlBindingAccessor(Func<ControlBinding> getter, Action<ControlBinding> setter)
        {
            Getter = getter;
            Setter = setter;
        }
    }

    public struct StringBindingAccessor
    {
        public Func<string> Getter;
        public Action<string> Setter;

        public StringBindingAccessor(Func<string> getter, Action<string> setter)
        {
            Getter = getter;
            Setter = setter;
        }
    }
}
