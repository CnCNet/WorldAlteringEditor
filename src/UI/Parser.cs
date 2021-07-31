﻿using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;

namespace TSMapEditor.UI
{
    /// <summary>
    /// Parses arithmetic expressions.
    /// </summary>
    class Parser
    {
        private const int CHAR_VALUE_ZERO = 48;

        public Parser(WindowManager windowManager)
        {
            if (_instance != null)
                throw new InvalidOperationException("Only one instance of Parser can exist at a time.");

            globalConstants = new Dictionary<string, int>();
            globalConstants.Add("RESOLUTION_WIDTH", windowManager.RenderResolutionX);
            globalConstants.Add("RESOLUTION_HEIGHT", windowManager.RenderResolutionY);
            globalConstants.Add("EMPTY_SPACE_TOP", Constants.UIEmptyTopSpace);
            globalConstants.Add("EMPTY_SPACE_BOTTOM", Constants.UIEmptyBottomSpace);
            globalConstants.Add("EMPTY_SPACE_SIDES", Constants.UIEmptySideSpace);
            globalConstants.Add("HORIZONTAL_SPACING", Constants.UIHorizontalSpacing);
            globalConstants.Add("VERTICAL_SPACING", Constants.UIVerticalSpacing);

            _instance = this;
        }

        private static Parser _instance;
        public static Parser Instance => _instance;

        private static Dictionary<string, int> globalConstants;

        public string Input { get; private set; }

        private int tokenPlace;
        private XNAControl primaryControl;
        private XNAControl parsingControl;

        private XNAControl GetControl(string controlName)
        {
            if (controlName == primaryControl.Name)
                return primaryControl;

            var control = Find(primaryControl.Children, controlName);
            if (control == null)
                throw new KeyNotFoundException($"Control '{controlName}' not found while parsing input '{Input}'");

            return control;
        }

        private XNAControl Find(IEnumerable<XNAControl> list, string controlName)
        {
            foreach (XNAControl child in list)
            {
                if (child.Name == controlName)
                    return child;

                XNAControl childOfChild = Find(child.Children, controlName);
                if (childOfChild != null)
                    return childOfChild;
            }

            return null;
        }

        private int GetConstant(string constantName)
        {
            return globalConstants[constantName];
        }

        public void SetPrimaryControl(XNAControl primaryControl)
        {
            this.primaryControl = primaryControl;
        }

        public int GetExprValue(string input, XNAControl parsingControl)
        {
            this.parsingControl = parsingControl;
            Input = input;
            tokenPlace = 0;
            return GetExprValue();
        }

        private int GetExprValue()
        {
            int value = 0;

            while (true)
            {
                SkipWhitespace();

                if (IsEndOfInput())
                    return value;

                char c = Input[tokenPlace];

                if (char.IsDigit(c))
                {
                    value = GetInt();
                }
                else if (c == '+')
                {
                    tokenPlace++;
                    value += GetNumericalValue();
                }
                else if (c == '-')
                {
                    tokenPlace++;
                    value -= GetNumericalValue();
                }
                else if (c == '/')
                {
                    tokenPlace++;
                    value /= GetExprValue();
                }
                else if (c == '*')
                {
                    tokenPlace++;
                    value *= GetExprValue();
                }
                else if (c == '(')
                {
                    tokenPlace++;
                    value = GetExprValue();
                }
                else if (c == ')')
                {
                    tokenPlace++;
                    return value;
                }
                else if (char.IsUpper(c))
                {
                    value = GetConstantValue();
                }
                else if (char.IsLower(c))
                {
                    value = GetFunctionValue();
                }
            }
        }

        private int GetNumericalValue()
        {
            SkipWhitespace();

            if (IsEndOfInput())
                return 0;

            char c = Input[tokenPlace];

            if (char.IsDigit(c))
            {
                return GetInt();
            }
            else if (char.IsUpper(c))
            {
                return GetConstantValue();
            }
            else if (char.IsLower(c))
            {
                return GetFunctionValue();
            }
            else if (c == '(')
            {
                tokenPlace++;
                return GetExprValue();
            }
            else
                throw new INIConfigException("Unexpected character " + c + " when parsing input: " + Input);
        }

        private void SkipWhitespace()
        {
            while (true)
            {
                if (IsEndOfInput())
                    return;

                char c = Input[tokenPlace];
                if (c == ' ' || c == '\r' || c == '\n')
                    tokenPlace++;
                else
                    break;
            }
        }

        private string GetIdentifier()
        {
            string identifierName = "";

            while (true)
            {
                if (IsEndOfInput())
                    break;

                char c = Input[tokenPlace];
                if (char.IsWhiteSpace(c))
                    break;

                if (!char.IsLetterOrDigit(c) && c != '_')
                    break;

                identifierName += c.ToString();
                tokenPlace++;
            }

            return identifierName;
        }

        private int GetConstantValue()
        {
            string constantName = GetIdentifier();
            return GetConstant(constantName);
        }

        private int GetFunctionValue()
        {
            string functionName = GetIdentifier();
            SkipWhitespace();
            ConsumeChar('(');
            string paramName = GetIdentifier();
            SkipWhitespace();
            ConsumeChar(')');

            switch (functionName)
            {
                case "getX":
                    return GetControl(paramName).X;
                case "getY":
                    return GetControl(paramName).Y;
                case "getWidth":
                    return GetControl(paramName).Width;
                case "getHeight":
                    return GetControl(paramName).Height;
                case "getBottom":
                    return GetControl(paramName).Bottom;
                case "getRight":
                    return GetControl(paramName).Right;
                case "horizontalCenterOnParent":
                    parsingControl.CenterOnParentHorizontally();
                    return parsingControl.X;
                default:
                    throw new INIConfigException("Unknown function " + functionName + " in expression " + Input);
            }
        }

        private void ConsumeChar(char token)
        {
            if (Input[tokenPlace] != token)
                throw new INIConfigException("Parse error: expected '" + token + "' in expression " + Input);
            tokenPlace++;
        }

        private int GetInt()
        {
            int value = 0;
            while (true)
            {
                if (IsEndOfInput())
                    return value;

                char c = Input[tokenPlace];
                if (!char.IsDigit(c))
                    return value;

                value = (value * 10) + Input[tokenPlace] - CHAR_VALUE_ZERO;
                tokenPlace++;
            }
        }

        private bool IsEndOfInput()
        {
            if (tokenPlace >= Input.Length)
                return true;

            return false;
        }
    }
}