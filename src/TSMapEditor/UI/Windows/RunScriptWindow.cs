using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.IO;
using TSMapEditor.Models;
using TSMapEditor.Scripts;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows
{
    public class RunScriptWindow : INItializableWindow
    {
        public RunScriptWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        public event EventHandler ScriptRun;

        private readonly Map map;

        private EditorListBox lbScriptFiles;
        private XNALabel lblHeader;
        private XNALabel lblDescription;
        private EditorButton btnRunScript;

        private string scriptPath;

        public override void Initialize()
        {
            Name = nameof(RunScriptWindow);
            base.Initialize();

            lbScriptFiles = FindChild<EditorListBox>(nameof(lbScriptFiles));
            lblHeader = FindChild<XNALabel>("lblHeader");
            lblDescription = FindChild<XNALabel>("lblDescription");
            btnRunScript = FindChild<EditorButton>("btnRunScript");
            
            btnRunScript.LeftClick += BtnRunScript_LeftClick;
            
            // 初始化时应用当前语言
            RefreshLanguage();
        }
        
        /// <summary>
        /// 刷新窗口的语言
        /// </summary>
        public void RefreshLanguage()
        {
            bool isChinese = MainMenu.IsChinese;
            
            if (isChinese)
            {
                // 设置窗口标题
                SetWindowTitle("运行脚本");
                
                // 更新标签文本
                if (lblHeader != null)
                    lblHeader.Text = "运行脚本";
                    
                if (lblDescription != null)
                    lblDescription.Text = "运行自定义脚本，可以强大地修改地图。\n\n注意：脚本是C#程序，可能包含恶意代码！\n只运行来自您信任的作者的脚本！";
                
                // 更新按钮文本
                if (btnRunScript != null)
                    btnRunScript.Text = "运行选定脚本";
            }
            else
            {
                // 设置窗口标题
                SetWindowTitle("Run Script");
                
                // 更新标签文本
                if (lblHeader != null)
                    lblHeader.Text = "Run Script";
                    
                if (lblDescription != null)
                    lblDescription.Text = "Run custom scripts that allow you to modify the map in powerful ways.\n\nNOTE: Scripts are C# programs and can contain malicious code!\nOnly run scripts from authors whom you trust!";
                
                // 更新按钮文本
                if (btnRunScript != null)
                    btnRunScript.Text = "Run Selected Script";
            }
        }
        
        // 通过修改INI配置设置窗口标题
        private void SetWindowTitle(string title)
        {
            try
            {
                // 使用反射获取ConfigIni
                var configIniField = GetType().BaseType.GetField("ConfigIni", 
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (configIniField != null)
                {
                    var configIni = configIniField.GetValue(this) as Rampastring.Tools.IniFile;
                    if (configIni != null)
                    {
                        var section = configIni.GetSection(Name);
                        if (section != null)
                        {
                            section.SetStringValue("WindowTitle", title);
                        }
                    }
                }
                
                // 尝试调用刷新布局方法
                var method = GetType().BaseType.GetMethod("RefreshLayout", 
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                if (method != null)
                {
                    method.Invoke(this, null);
                }
            }
            catch
            {
                // 如果反射操作失败，静默忽略
            }
        }

        private void BtnRunScript_LeftClick(object sender, EventArgs e)
        {
            if (lbScriptFiles.SelectedItem == null)
                return;

            string filePath = (string)lbScriptFiles.SelectedItem.Tag;
            if (!File.Exists(filePath))
            {
                bool isChinese = MainMenu.IsChinese;
                if (isChinese)
                    EditorMessageBox.Show(WindowManager, "找不到文件",
                        "选定的文件不存在！可能已被删除？", MessageBoxButtons.OK);
                else
                    EditorMessageBox.Show(WindowManager, "Can't find file",
                        "The selected file does not exist! Maybe it was deleted?", MessageBoxButtons.OK);

                return;
            }

            scriptPath = filePath;

            (string error, string confirmation) = ScriptRunner.GetDescriptionFromScript(map, filePath);

            if (error != null)
            {
                Logger.Log("Compilation error when attempting to run fetch script description: " + error);
                
                bool isChinese = MainMenu.IsChinese;
                if (isChinese)
                    EditorMessageBox.Show(WindowManager, "错误",
                        "编译脚本失败！请检查其语法，或联系其作者获取支持。" + Environment.NewLine + Environment.NewLine +
                        "返回的错误为：" + error, MessageBoxButtons.OK);
                else
                    EditorMessageBox.Show(WindowManager, "Error",
                        "Compiling the script failed! Check its syntax, or contact its author for support." + Environment.NewLine + Environment.NewLine +
                        "Returned error was: " + error, MessageBoxButtons.OK);
                return;
            }

            if (confirmation == null)
            {
                bool isChinese = MainMenu.IsChinese;
                if (isChinese)
                    EditorMessageBox.Show(WindowManager, "错误", "脚本没有提供描述！", MessageBoxButtons.OK);
                else
                    EditorMessageBox.Show(WindowManager, "Error", "The script provides no description!", MessageBoxButtons.OK);
                return;
            }

            confirmation = Renderer.FixText(confirmation, Constants.UIDefaultFont, Width).Text;

            bool isCh = MainMenu.IsChinese;
            EditorMessageBox messageBox;
            if (isCh)
                messageBox = EditorMessageBox.Show(WindowManager, "确定吗？", confirmation, MessageBoxButtons.YesNo);
            else
                messageBox = EditorMessageBox.Show(WindowManager, "Are you sure?", confirmation, MessageBoxButtons.YesNo);
            messageBox.YesClickedAction = (_) => ApplyCode();
        }

        private void ApplyCode()
        {
            if (scriptPath == null)
                throw new InvalidOperationException("Pending script path is null!");

            string result = ScriptRunner.RunScript(map, scriptPath);
            result = Renderer.FixText(result, Constants.UIDefaultFont, Width).Text;

            bool isChinese = MainMenu.IsChinese;
            if (isChinese)
                EditorMessageBox.Show(WindowManager, "结果", result, MessageBoxButtons.OK);
            else
                EditorMessageBox.Show(WindowManager, "Result", result, MessageBoxButtons.OK);
            ScriptRun?.Invoke(this, EventArgs.Empty);
        }

        public void Open()
        {
            lbScriptFiles.Clear();

            string directoryPath = Path.Combine(Environment.CurrentDirectory, "Config", "Scripts");

            if (!Directory.Exists(directoryPath))
            {
                Logger.Log("WAE scipts directory not found!");
                
                bool isChinese = MainMenu.IsChinese;
                if (isChinese)
                    EditorMessageBox.Show(WindowManager, "错误", "找不到脚本目录！\r\n\r\n期望的路径：" + directoryPath, MessageBoxButtons.OK);
                else
                    EditorMessageBox.Show(WindowManager, "Error", "Scripts directory not found!\r\n\r\nExpected path: " + directoryPath, MessageBoxButtons.OK);
                return;
            }

            var iniFiles = Directory.GetFiles(directoryPath, "*.cs");

            foreach (string filePath in iniFiles)
            {
                lbScriptFiles.AddItem(new XNAListBoxItem(Path.GetFileName(filePath)) { Tag = filePath });
            }
            
            // 打开窗口时刷新语言
            RefreshLanguage();
            Show();
        }
    }
}
