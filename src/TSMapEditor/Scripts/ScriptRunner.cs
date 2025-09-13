using Rampastring.Tools;
using Rampastring.XNAUI;
using System;
using System.IO;
using System.Reflection;
using TSMapEditor.Models;
using TSMapEditor.Rendering;
using TSMapEditor.UI;
using TSMapEditor.UI.Windows;
using Westwind.Scripting;

namespace TSMapEditor.Scripts
{
    // Has to be a class, Westwind.Scripting does not appear to recognize this if it is a struct.
    public class ScriptDependencies
    {
        public Map Map;
        public ICursorActionTarget CursorActionTarget;
        public EditorState EditorState;
        public WindowManager WindowManager;
        public WindowController WindowController;

        public ScriptDependencies(Map map, ICursorActionTarget cursorActionTarget, EditorState editorState, WindowManager windowManager, WindowController windowController)
        {
            Map = map;
            CursorActionTarget = cursorActionTarget;
            EditorState = editorState;
            WindowManager = windowManager;
            WindowController = windowController;
        }
    }

    public static class ScriptRunner
    {
        private static object scriptClassInstance;

        private static MethodInfo getDescriptionMethod; // V1 only
        private static MethodInfo performMethod;
        private static MethodInfo getSuccessMessageMethod; // V1 only

        public static int ActiveScriptAPIVersion;

        public static string CompileScript(ScriptDependencies scriptDependencies, string scriptPath)
        {
            if (!File.Exists(scriptPath))
                return Translate("ScriptRunner.CompileScript.NoScriptFile", "The script file does not exist!");

            var sourceCode = File.ReadAllText(scriptPath);
            string error = CompileSource(scriptDependencies, sourceCode);
            if (error != null)
                return error;

            return null;
        }

        public static string GetDescriptionFromScriptV1()
        {
            return (string)getDescriptionMethod.Invoke(scriptClassInstance, null);
        }

        public static string RunScriptV1(Map map, string scriptPath)
        {
            if (scriptClassInstance == null || performMethod == null || getSuccessMessageMethod == null)
                throw new InvalidOperationException("Script not properly compiled!");

            Logger.Log("Running script from " + scriptPath);

            try
            {
                performMethod.Invoke(scriptClassInstance, new object[] { map });
                return (string)getSuccessMessageMethod.Invoke(scriptClassInstance, null);
            }
            catch (Exception ex) // catching Exception is OK, we cannot know what the script can throw
            {
                string errorMessage = ex.Message;

                while (ex.InnerException != null)
                {
                    ex = ex.InnerException;
                    errorMessage += Environment.NewLine + Environment.NewLine + 
                        Translate("ScriptRunner.RunScriptV1.InnerException.Message", "Inner exception message: ") +
                        ex.Message + Environment.NewLine + 
                        Translate("ScriptRunner.RunScriptV1.InnerException.StackTrace", "Stack trace: ") + ex.StackTrace;
                }

                Logger.Log("Exception while running script. Returned exception message: " + errorMessage);

                return Translate("ScriptRunner.RunScriptV1.Exception.Message", "An error occurred while running the script. Returned error message: ") + 
                    Environment.NewLine + Environment.NewLine + 
                    errorMessage;
            }
        }

        public static string RunScriptV2()
        {
            try
            {
                performMethod.Invoke(scriptClassInstance, null);
            }
            catch (Exception ex) // catching Exception is OK, we cannot know what the script can throw
            {
                string errorMessage = ex.Message;

                while (ex.InnerException != null)
                {
                    ex = ex.InnerException;
                    errorMessage += Environment.NewLine + Environment.NewLine +
                        Translate("ScriptRunner.RunScriptV2.InnerException.Message", "Inner exception message: ") +
                         ex.Message + Environment.NewLine +
                        Translate("ScriptRunner.RunScriptV2.InnerException.StackTrace", "Stack trace: ") + ex.StackTrace;
                }

                Logger.Log("Exception while running script. Returned exception message: " + errorMessage);

                return Translate("ScriptRunner.RunScriptV2.Exception.Message", "An error occurred while running the script. Returned error message: ") +
                    Environment.NewLine + Environment.NewLine +
                    errorMessage;
            }

            return null;
        }

        private static string CompileSource(ScriptDependencies scriptDependencies, string source)
        {
            var script = new CSharpScriptExecution() { SaveGeneratedCode = true };
            script.AddLoadedReferences();
            script.AddNamespace("TSMapEditor");
            script.AddNamespace("TSMapEditor.Models");
            script.AddNamespace("TSMapEditor.Rendering");
            script.AddNamespace("TSMapEditor.GameMath");
            script.AddNamespace("TSMapEditor.Scripts");
            script.AddNamespace("TSMapEditor.UI");
            script.AddNamespace("TSMapEditor.UI.Controls");

            getDescriptionMethod = null;
            performMethod = null;
            getSuccessMessageMethod = null;

            object instance = script.CompileClass(source);

            if (script.Error)
            {
                return script.ErrorMessage;
            }

            int version = 1;
            Type classType = instance.GetType();
            var properties = classType.GetProperties();
            var apiVersionProperty = Array.Find(properties, prop => prop.Name == "ApiVersion");
            if (apiVersionProperty != null)
            {
                if (apiVersionProperty.PropertyType != typeof(int))
                {
                    return Translate("ScriptRunner.CompileSource.ApiVersionNotInt", "ApiVersion property is not an integer!");
                }

                version = (int)apiVersionProperty.GetValue(instance);
            }

            ActiveScriptAPIVersion = version;

            if (version == 1)
            {
                return ExtractScriptV1(instance);
            }
            else if (version == 2)
            {
                return ExtractScriptV2(instance, scriptDependencies);
            }

            return string.Format(Translate("ScriptRunner.CompileSource.UnsupportedApiVersion", 
                "Unsupported scripting API version: {0}. Contact the script's author for troubleshooting."), version);
        }

        private static string ExtractScriptV2(object instance, ScriptDependencies scriptDependencies)
        {
            scriptClassInstance = instance;
            Type classType = instance.GetType();

            var methods = classType.GetMethods();
            foreach (MethodInfo method in methods)
            {
                if (method.Name == "Perform")
                {
                    performMethod = method;
                    if (performMethod.GetParameters().Length > 0)
                    {
                        return Translate("ScriptRunner.ExtractScriptV2.PerformParams", "The Perform method has one or more parameters." + Environment.NewLine +
                            "It should have no parameters in a V2 script." + Environment.NewLine +
                            "To access map data, access ScriptDependencies.Map.");
                    }
                }
            }

            var properties = classType.GetProperties();
            foreach (PropertyInfo property in properties)
            {
                var setter = property.GetSetMethod();
                if (setter == null)
                    continue;

                if (property.Name == "ScriptDependencies")
                {
                    setter.Invoke(instance, [scriptDependencies]);
                }
            }

            if (performMethod == null)
            {
                return Translate("ScriptRunner.ExtractScriptV2.NoPerformMethod", "The script does not declare the Perform method.");
            }

            return null;
        }

        private static string ExtractScriptV1(object instance)
        {
            scriptClassInstance = instance;
            Type classType = instance.GetType();

            var methods = classType.GetMethods();
            foreach (MethodInfo method in methods)
            {
                if (method.Name == "GetDescription")
                {
                    getDescriptionMethod = method;

                    if (getDescriptionMethod.ReturnType != typeof(string))
                        return Translate("ScriptRunner.ExtractScriptV1.DescriptionReturnType", "GetDescription does not return a string!");
                }
                else if (method.Name == "Perform")
                {
                    performMethod = method;
                }
                else if (method.Name == "GetSuccessMessage")
                {
                    getSuccessMessageMethod = method;

                    if (getSuccessMessageMethod.ReturnType != typeof(string))
                        return Translate("ScriptRunner.ExtractScriptV1.SuccessMessageReturnType", "GetSuccessMessage does not return a string!");
                }
            }

            if (getDescriptionMethod == null || performMethod == null || getSuccessMessageMethod == null)
            {
                return Translate("ScriptRunner.ExtractScriptV1.MissingRequiredMethods", "The script does not declare one or more required methods.");
            }

            return null;
        }
    }
}
