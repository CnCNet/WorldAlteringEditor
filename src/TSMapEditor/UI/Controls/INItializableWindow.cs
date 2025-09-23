using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TSMapEditor.UI.Controls
{
    /// <summary>
    /// A base class for windows that can create themselves through an INI configuration file.
    /// </summary>
    public class INItializableWindow : EditorWindow
    {
        public INItializableWindow(WindowManager windowManager) : base(windowManager)
        {
        }

        protected IniFile ConfigIni { get; set; }

        private bool _initialized = false;

        protected bool HasCloseButton { get; set; }

        protected EditorButton btnClose { get; private set; }

        protected string SubDirectory { get; set; } = "Windows";

        public bool CenterByDefault { get; set; } = true;

        public T FindChild<T>(string childName, bool optional = false) where T : XNAControl
        {
            T child = FindChild<T>(Children, childName);
            if (child == null && !optional)
                throw new KeyNotFoundException("Could not find required child control: " + childName);

            return child;
        }

        private T FindChild<T>(IEnumerable<XNAControl> list, string controlName) where T : XNAControl
        {
            foreach (XNAControl child in list)
            {
                if (child.Name == controlName)
                    return (T)child;

                XNAControl childOfChild = FindChild<T>(child.Children, controlName);
                if (childOfChild != null)
                    return (T)childOfChild;
            }

            return null;
        }

        public override void Initialize()
        {
            if (_initialized)
                throw new InvalidOperationException("INItializableWindow cannot be initialized twice.");

            var dsc = Path.DirectorySeparatorChar;

            if (ConfigIni == null)
            {
                string defaultConfigIniPath = Path.Combine(Environment.CurrentDirectory, "Config", "Default", "UI", SubDirectory, Name + ".ini");
                string configIniPath = Path.Combine(Environment.CurrentDirectory, "Config", "UI", SubDirectory, Name + ".ini");

                if (File.Exists(configIniPath))
                    ConfigIni = new IniFile(configIniPath);
                else if (File.Exists(defaultConfigIniPath))
                    ConfigIni = new IniFile(defaultConfigIniPath);
                else
                    throw new FileNotFoundException("Config INI not found: " + configIniPath);
            }

            Parser.Instance.SetPrimaryControl(this);
            ReadINIForControl(this);
            ReadLateAttributesForControl(this);

            base.Initialize();

            if (HasCloseButton)
            {
                btnClose = new EditorButton(WindowManager);
                btnClose.Name = "btnCloseX";
                btnClose.Width = Constants.UIButtonHeight;
                btnClose.Height = Constants.UIButtonHeight;
                btnClose.Text = "X";
                btnClose.X = Width - btnClose.Width;
                btnClose.Y = 0;
                AddChild(btnClose);
                btnClose.LeftClick += (s, e) => Hide();
            }

            _initialized = true;
        }

        protected override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
        {
            if (key == nameof(HasCloseButton))
                HasCloseButton = iniFile.GetBooleanValue(Name, key, HasCloseButton);
            
            base.ParseControlINIAttribute(iniFile, key, value);
        }

        public void RefreshLayout()
        {
            Parser.Instance.SetPrimaryControl(this);

            var controls = new List<XNAControl>() { this }.Concat(Children);

            foreach (var control in controls)
            {
                ReadINIForControl(control, true);
            }

            if (btnClose != null)
                btnClose.X = Width - btnClose.Width;

            if (CenterByDefault)
                CenterOnParent();
        }

        private bool ReadINIForControl(XNAControl control, bool isForLayout = false)
        {
            var section = ConfigIni.GetSection(control.Name);
            if (section == null)
                return false;

            foreach (var kvp in section.Keys)
            {
                if (kvp.Key.StartsWith("$CC"))
                {
                    if (!isForLayout)
                    {
                        var child = CreateChildControl(control, kvp.Value);
                        if (!ReadINIForControl(child))
                            throw new INIConfigException("No section exists for child control " + kvp.Value);

                        child.Initialize();
                    }
                    else
                    {
                        string childName = GetChildControlName(control, kvp.Value);
                        var child = control.Children.First(cc => cc.Name == childName);
                        if (child == null)
                            throw new INIConfigException($"Processing {control.Name} in {nameof(INItializableWindow)}: Unable to find child control {kvp.Value} while calculating layout");

                        ReadINIForControl(child, true);
                    }
                }
                else if (kvp.Key == "$X")
                {
                    control.X = Parser.Instance.GetExprValue(kvp.Value, control);
                }
                else if (kvp.Key == "$Y")
                {
                    control.Y = Parser.Instance.GetExprValue(kvp.Value, control);
                }
                else if (kvp.Key == "$Width")
                {
                    control.Width = Parser.Instance.GetExprValue(kvp.Value, control);
                }
                else if (kvp.Key == "$Height")
                {
                    control.Height = Parser.Instance.GetExprValue(kvp.Value, control);
                }
                else if (kvp.Key == "$Text")
                {
                    control.Text = Parser.Instance.GetExprValueString(kvp.Value, "Text", control);
                }
                else if (kvp.Key == "$Suggestion" && control is XNASuggestionTextBox)
                {
                    ((XNASuggestionTextBox)control).Suggestion = Parser.Instance.GetExprValueString(kvp.Value, "Suggestion", control);
                }
                else if (kvp.Key == "$TextAnchor" && control is XNALabel)
                {
                    // TODO refactor these to be more object-oriented
                    ((XNALabel)control).TextAnchor = (LabelTextAnchorInfo)Enum.Parse(typeof(LabelTextAnchorInfo), kvp.Value);
                }
                else if (kvp.Key == "$AnchorPoint" && control is XNALabel)
                {
                    string[] parts = kvp.Value.Split(',');
                    if (parts.Length != 2)
                        throw new FormatException("Invalid format for AnchorPoint: " + kvp.Value);
                    ((XNALabel)control).AnchorPoint = new Vector2(Parser.Instance.GetExprValue(parts[0], control), Parser.Instance.GetExprValue(parts[1], control));
                }
                else if (!isForLayout && kvp.Key == "$MaxValue" && control is XNATrackbar)
                {
                    ((XNATrackbar)control).MaxValue = Parser.Instance.GetExprValue(kvp.Value, control);
                }
                else if (!isForLayout && kvp.Key == "$Enabled")
                {
                    int value = Parser.Instance.GetExprValue(kvp.Value, control);
                    if (value < 1)
                        control.Disable();
                    else
                        control.Enable();
                }
                else if (!isForLayout && kvp.Key == "$LeftClickAction")
                {
                    if (kvp.Value == "Disable")
                    {
                        control.LeftClick += (s, e) => Hide();
                    }
                }
                else if (!isForLayout)
                {
                    control.ParseINIAttribute(ConfigIni, kvp.Key, kvp.Value);
                }
            }

            return true;
        }

        /// <summary>
        /// Reads a second set of attributes for a control's child controls.
        /// Enables linking controls to controls that are defined after them.
        /// </summary>
        private void ReadLateAttributesForControl(XNAControl control)
        {
            var section = ConfigIni.GetSection(control.Name);
            if (section == null)
                return;

            var children = control.Children.ToList();
            foreach (var child in children)
            {
                var childSection = ConfigIni.GetSection(child.Name);

                if (childSection != null)
                {
                    // This logic should also be enabled for other types in the future,
                    // but it requires changes in XNAUI
                    if (child is XNATextBox)
                    {
                        string nextControl = childSection.GetStringValue("NextControl", null);

                        if (!string.IsNullOrWhiteSpace(nextControl))
                        {
                            var otherChild = children.Find(c => c.Name == nextControl);
                            if (otherChild != null)
                            {
                                ((XNATextBox)child).NextControl = otherChild;

                                if (otherChild is XNATextBox otherAsTb)
                                {
                                    if (otherAsTb.PreviousControl == null)
                                        otherAsTb.PreviousControl = child;
                                }
                            }
                        }

                        string previousControl = childSection.GetStringValue("PreviousControl", null);
                        if (!string.IsNullOrWhiteSpace(previousControl))
                        {
                            var otherChild = children.Find(c => c.Name == previousControl);
                            if (otherChild != null)
                                ((XNATextBox)child).PreviousControl = otherChild;
                        }
                    }

                    string toolTipText = childSection.GetStringValue("ToolTip", null);
                    if (!string.IsNullOrWhiteSpace(toolTipText))
                    {
                        var toolTipControl = new ToolTip(WindowManager, child);
                        toolTipControl.Text = toolTipText;
                        toolTipControl.ToolTipDelay = 0;
                    }

                    string parsedToolTipText = childSection.GetStringValue("$ToolTip", null);
                    if (parsedToolTipText != null)
                    {
                        parsedToolTipText = Parser.Instance.GetExprValueString(parsedToolTipText, "ToolTip", child);
                        var toolTipControl = new ToolTip(WindowManager, child);
                        toolTipControl.Text = parsedToolTipText;
                        toolTipControl.ToolTipDelay = 0;
                    }
                }
                    
                ReadLateAttributesForControl(child);
            }
        }

        private XNAControl CreateChildControl(XNAControl parent, string keyValue)
        {
            string[] parts = keyValue.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
            string childName = GetChildControlName(parent, keyValue);

            if (FindChild<XNAControl>(childName, true) != null)
            {
                throw new INIConfigException("A control named " + childName + " has been defined more than once.");
            }

            var childControl = EditorGUICreator.Instance.CreateControl(WindowManager, parts[1]);
            childControl.Name = childName;
            parent.AddChildWithoutInitialize(childControl);
            return childControl;
        }

        private string GetChildControlName(XNAControl parent, string keyValue)
        {
            string[] parts = keyValue.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 2)
                throw new INIConfigException("Invalid child control definition " + keyValue);

            if (string.IsNullOrWhiteSpace(parts[0]))
                throw new INIConfigException("Empty name in child control definition for " + parent.Name);

            return parts[0];
        }
    }
}
