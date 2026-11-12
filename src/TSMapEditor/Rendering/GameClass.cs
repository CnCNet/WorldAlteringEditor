global using static MapEditorLibrary.Misc.Translator;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using Rampastring.XNAUI;
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
#if WINDOWS
using System.Windows.Forms;
using TSMapEditor.UI.IME;
#endif
using TSMapEditor.Misc;
using TSMapEditor.Settings;
using TSMapEditor.UI;
using MapEditorLibrary;
using MapEditorLibrary.Misc;
using MapEditorLibrary.CCEngine;

namespace TSMapEditor.Rendering;

public class GameClass : Microsoft.Xna.Framework.Game
{
    private GraphicsDeviceManager graphics;
    private PowerSaveManager powerSaveManager;

    public GameClass()
    {
#if !DEBUG
        AppDomain.CurrentDomain.UnhandledException += (s, e) => HandleUnhandledException((Exception)e.ExceptionObject);
        Application.ThreadException += (s, e) => HandleUnhandledException(e.Exception);
#endif
        Program.DisableExceptionHandler();

        Logger.WriteToConsole = true;
        Logger.WriteLogFile = true;
        Logger.Initialize(Environment.CurrentDirectory + "/", "MapEditorLog.log");

        try
        {
            File.Delete(Environment.CurrentDirectory + "/MapEditorLog.log");
        }
        catch (IOException ex)
        {
            Logger.Log("Failed to delete log file! Returned error: " + ex.Message);
        }

        Logger.Log("C&C World-Altering Editor (WAE) version " + Constants.Version);

        AutoLATType.InitArray();

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        TranslatorSetup.LoadTranslations();
        Constants.Init();
        new UserSettings();
        TranslatorSetup.SetActiveTranslation(UserSettings.Instance.Language);

        AutosaveTimer.Purge();

        graphics = new GraphicsDeviceManager(this);
        graphics.HardwareModeSwitch = false;
        graphics.GraphicsProfile = GraphicsProfile.HiDef;
        graphics.PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8;
        Content.RootDirectory = "Content";
        graphics.SynchronizeWithVerticalRetrace = false;
        Window.Title = "C&C World-Altering Editor (WAE)";
    }

    private void HandleUnhandledException(Exception ex)
    {
        string exceptLogPath = Environment.CurrentDirectory + DSC + "except.txt";
        File.Delete(exceptLogPath);

        StringBuilder sb = new StringBuilder();

        string fullName = typeof(GameClass).Assembly.FullName;

        LogLineGenerate("World-Altering Editor (" + fullName + ")", sb, exceptLogPath);
        LogLineGenerate("Version: " + Constants.Version, sb, exceptLogPath);
        LogLineGenerate("Unhandled exception! @ " + DateTime.Now.ToLongTimeString(), sb, exceptLogPath);
        LogLineGenerate("Message: " + ex.Message, sb, exceptLogPath);
        LogLineGenerate("Stack trace: " + ex.StackTrace, sb, exceptLogPath);

        if (ex.InnerException != null)
        {
            LogLineGenerate("***************************", sb, exceptLogPath);
            LogLineGenerate("InnerException information:", sb, exceptLogPath);
            LogLineGenerate("Message: " + ex.InnerException.Message, sb, exceptLogPath);
            LogLineGenerate("Stack trace: " + ex.InnerException.StackTrace, sb, exceptLogPath);
        }

        Logger.Log("Exiting.");

        windowManager?.HideWindow();
        System.Windows.Forms.MessageBox.Show("The map editor has crashed." + Environment.NewLine + Environment.NewLine +
            "Exception information logged into except.txt:" + Environment.NewLine + Environment.NewLine +
            sb.ToString());

        Environment.Exit(255);
    }

    private void LogLineGenerate(string text, StringBuilder sb, string exceptLogPath)
    {
        sb.Append(text + Environment.NewLine);
        Logger.ForceLog(text, exceptLogPath);
    }

    private WindowManager windowManager;

    private readonly char DSC = Path.DirectorySeparatorChar;

    protected override void Initialize()
    {
        base.Initialize();

        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

        AssetLoader.Initialize(GraphicsDevice, Content);
        AssetLoader.AssetSearchPaths.Add(Path.Combine(Environment.CurrentDirectory, "Config", "Translations", TranslatorSetup.ActiveTranslationDirectory()));
        AssetLoader.AssetSearchPaths.Add(Environment.CurrentDirectory + DSC + "Content" + DSC);

        // Hack: allow translations to override fonts
        // int i = 0;
        // while (true)
        // {
        //     string spriteFontPath = Path.Combine("Translations", TranslatorSetup.ActiveTranslationDirectory(), "SpriteFont" + i + ".xnb");
        //     if (AssetLoader.AssetExists(Path.Combine(Environment.CurrentDirectory, "Config", spriteFontPath)))
        //     {
        //         var spriteFont = Content.Load<SpriteFont>(spriteFontPath);
        //         Renderer.GetFontList()[i] = spriteFont;
        //     }
        //     else
        //     {
        //         break;
        //     }
        // }

        windowManager = new WindowManager(this, graphics);
        windowManager.Initialize(Content, Environment.CurrentDirectory + DSC + "Content" + DSC);

        new Parser(windowManager);

        const int menuRenderWidth = 800;
        const int menuRenderHeight = 600;

        int menuWidth = menuRenderWidth;
        int menuHeight = menuRenderHeight;

        int dpi = NativeMethods.GetScreenDPI();
        double dpi_ratio = dpi / 96.0;

        menuWidth = (int)(menuWidth * dpi_ratio);
        menuHeight = (int)(menuHeight * dpi_ratio);

#if WINDOWS
        // If the user has a very large display at 100% DPI, it's probably best to integer-upscale the main menu.
        if (dpi_ratio <= 1.0)
        {
            const int minScaleRatio = 3;
            if (Screen.PrimaryScreen.Bounds.Width >= menuRenderWidth * minScaleRatio &&
                Screen.PrimaryScreen.Bounds.Height >= menuRenderHeight * minScaleRatio)
            {
                menuWidth = menuRenderWidth * 2;
                menuHeight = menuRenderHeight * 2;
            }
        }
#endif

        windowManager.InitGraphicsMode(menuWidth, menuHeight, false);

        windowManager.SetRenderResolution(menuRenderWidth, menuRenderHeight);
        windowManager.CenterOnScreen();
        windowManager.Cursor.LoadNativeCursor(Environment.CurrentDirectory + DSC + "Content" + DSC + "cursor.cur");
        windowManager.SetBorderlessMode(false);

        Components.Add(windowManager);

        powerSaveManager = new PowerSaveManager(this);
        windowManager.PowerSaveManager = powerSaveManager;

        EditorThemes.Initialize();
        UISettings.ActiveSettings = new CustomUISettings()
        {
            PanelBackgroundColor = new Color(32, 32, 32),
            PanelBorderColor = new Color(196, 196, 196)
        };

#if WINDOWS
        IMEHandler imeHandler = IMEHandler.Create(this);
        windowManager.IMEHandler = imeHandler;
#endif

        InitMainMenu();
    }

    private void InitMainMenu()
    {
        var mainMenuWrapper = new MainMenuWrapper(windowManager);
        windowManager.AddAndInitializeControl(mainMenuWrapper);
        windowManager.CenterControlOnScreen(mainMenuWrapper);
    }

    protected override void Update(GameTime gameTime)
    {
        powerSaveManager.Update();

        base.Update(gameTime);
    }

    protected override bool BeginDraw()
    {
        return powerSaveManager.ShouldDraw();
    }
}
