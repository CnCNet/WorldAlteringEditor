using Microsoft.Xna.Framework.Input;
using Rampastring.Tools;
using Rampastring.XNAUI.Input;
using System;

namespace TSMapEditor.UI
{
    [Flags]
    public enum KeyboardModifiers
    {
        None = 0,
        Shift = 1,
        Ctrl = 2,
        Alt = 4
    }

    public class KeyboardCommandInput
    {
        public KeyboardCommandInput(Keys key, KeyboardModifiers modifiers)
        {
            Key = key;
            Modifiers = modifiers;
        }

        public Keys Key;
        public KeyboardModifiers Modifiers;

        public string GetKeyDisplayString(bool allowModifiersOnly)
        {
            string key = "";

            if (Key == Keys.None)
            {
                if (!allowModifiersOnly || Modifiers == KeyboardModifiers.None)
                    return "";

                // Build a different kind of string when there's only modifiers

                if ((Modifiers & KeyboardModifiers.Ctrl) == KeyboardModifiers.Ctrl)
                    key += Translate(this, "CTRL", "CTRL");

                if ((Modifiers & KeyboardModifiers.Shift) == KeyboardModifiers.Shift)
                {
                    if (key.Length > 0)
                        key += Translate(this, "PlusWithShift", "+ Shift");
                    else
                        key += Translate(this, "Shift", "Shift");
                }

                if ((Modifiers & KeyboardModifiers.Alt) == KeyboardModifiers.Alt)
                {
                    if (key.Length > 0)
                        key += Translate(this, "PlusWithAlt", "+ Alt");
                    else
                        key += Translate(this, "Alt", "Alt");
                }

                return key;
            }

            // Add modifiers to key display string
            if ((Modifiers & KeyboardModifiers.Ctrl) == KeyboardModifiers.Ctrl)
                key += Translate(this, "CTRLWithPlus", "CTRL + ");

            if ((Modifiers & KeyboardModifiers.Shift) == KeyboardModifiers.Shift)
                key += Translate(this, "ShiftWithPlus", "Shift + ");

            if ((Modifiers & KeyboardModifiers.Alt) == KeyboardModifiers.Alt)
                key += Translate(this, "AltWithPlus", "Alt + ");

            return key + Key.ToString();
        }

        public string GetDataString()
        {
            return Key.ToString() + ":" + ((int)Modifiers).ToString();
        }

        public void ApplyDataString(string str)
        {
            string[] parts = str.Split(':');
            if (parts.Length != 2)
                throw new ArgumentException("Incorrect KeyboardCommandInput data format: " + str);

            Key = (Keys)Enum.Parse(typeof(Keys), parts[0]);
            Modifiers = (KeyboardModifiers)Conversions.IntFromString(parts[1], 0);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is KeyboardCommandInput objAsInput))
                return false;

            return objAsInput.Key == Key && objAsInput.Modifiers == Modifiers;
        }

        public override int GetHashCode()
        {
            return (int)Modifiers * 10000 + (int)Key;
        }
    }

    public class KeyboardCommand
    {
        public KeyboardCommand(string iniName, string uiName, KeyboardCommandInput defaultKey, bool allowedWithModifiersOnly = false, bool forActionsOnly = false)
        {
            ININame = iniName;
            UIName = uiName;
            AllowedWithModifiersOnly = allowedWithModifiersOnly;
            DefaultKey = defaultKey;
            Key = new KeyboardCommandInput(defaultKey.Key, defaultKey.Modifiers);
            ForActionsOnly = forActionsOnly;
        }

        public event EventHandler Triggered;

        public string ININame { get; }
        public string UIName { get; }
        public bool AllowedWithModifiersOnly { get; }
        public bool ForActionsOnly { get; }
        public KeyboardCommandInput DefaultKey { get; }
        public KeyboardCommandInput Key { get; set; }

        private Action action;
        public Action Action
        {
            get => action;
            set
            {
                if (action != null && value != null)
                    throw new InvalidOperationException("KeyboardCommand.Action can only be set once.");

                action = value;
            }
        }

        public void ClearSubscriptions()
        {
            Triggered = null;
        }

        public void DoTrigger()
        {
            Triggered?.Invoke(this, EventArgs.Empty);
        }

        public string GetKeyDisplayString()
        {
            return Key.GetKeyDisplayString(AllowedWithModifiersOnly);
        }

        private bool AreModifiersDown(RKeyboard keyboard)
        {
            if ((Key.Modifiers & KeyboardModifiers.Alt) == KeyboardModifiers.Alt && !keyboard.IsAltHeldDown())
                return false;

            if ((Key.Modifiers & KeyboardModifiers.Ctrl) == KeyboardModifiers.Ctrl && !keyboard.IsCtrlHeldDown())
                return false;

            if ((Key.Modifiers & KeyboardModifiers.Shift) == KeyboardModifiers.Shift && !keyboard.IsShiftHeldDown())
                return false;

            return true;
        }

        /// <summary>
        /// Checks if the keys of this command are held down.
        /// </summary>
        /// <param name="keyboard">The Rampastring.XNAUI RKeyboard instance.</param>
        /// <returns>True if the keys are currently held down, otherwise false.</returns>
        public bool AreKeysDown(RKeyboard keyboard)
        {
            if (Key.Key == Keys.None)
                return false;

            if (!keyboard.IsKeyHeldDown(Key.Key))
                return false;

            if (!AreModifiersDown(keyboard))
                return false;

            return true;
        }

        /// <summary>
        /// Checks if the keys of this command are held down.
        /// If the primary key of this command is None, it's ignored
        /// and only the key modifiers determine whether the key is held down.
        /// 
        /// If the primary key is None and no modifiers are specified,
        /// this function returns False regardless of the state of the user's keyboard.
        /// </summary>
        /// <param name="keyboard">The Rampastring.XNAUI RKeyboard instance.</param>
        /// <returns>True if the keys are currently held down, otherwise false.</returns>
        public bool AreKeysOrModifiersDown(RKeyboard keyboard)
        {
            if (Key.Key == Keys.None)
            {
                if (Key.Modifiers == KeyboardModifiers.None)
                    return false;
            }
            else
                return AreKeysDown(keyboard);

            return AreModifiersDown(keyboard);
        }
    }
}
