using Rampastring.Tools;
using System;
using System.Collections.Generic;
using TSMapEditor.Models;

namespace TSMapEditor.Extensions
{
    public static class ListExtensions
    {
        public static void ReadTaskForces(this List<TaskForce> taskForceList, IniFile iniFile, Rules rules, Action<string> errorLogger)
        {
            var section = iniFile.GetSection("TaskForces");
            if (section == null)
                return;

            foreach (var kvp in section.Keys)
            {
                if (string.IsNullOrWhiteSpace(kvp.Value))
                    continue;

                var taskForce = TaskForce.ParseTaskForce(rules, iniFile.GetSection(kvp.Value));
                if (taskForce == null)
                {
                    errorLogger(string.Format(Translate("ListExtensions.TaskForceParseError", 
                        "Failed to load TaskForce {0}. It might be missing a section or be otherwise invalid."), kvp.Value));

                    continue;
                }

                int existingIndex = taskForceList.FindIndex(tf => tf.ININame == kvp.Value);
                if (existingIndex > -1)
                {
                    taskForceList[existingIndex] = taskForce;
                }
                else
                {
                    taskForceList.Add(taskForce);
                }
            }
        }

        public static void ReadScripts(this List<Script> scriptList, IniFile iniFile, Action<string> errorLogger)
        {
            var section = iniFile.GetSection("ScriptTypes");
            if (section == null)
                return;

            var editorInfoSection = iniFile.GetSection("EditorScriptInfo");

            foreach (var kvp in section.Keys)
            {
                var script = Script.ParseScript(kvp.Value, iniFile.GetSection(kvp.Value));

                if (script == null)
                {
                    errorLogger(string.Format(Translate("ListExtensions.ScriptParseError", 
                        "Failed to load Script {0}. It might be missing a section or be otherwise invalid."), kvp.Value));

                    continue;
                }

                if (editorInfoSection != null)
                {
                    script.EditorColor = editorInfoSection.GetStringValue(script.ININame, null);
                }

                int existingIndex = scriptList.FindIndex(tf => tf.ININame == kvp.Value);
                if (existingIndex > -1)
                {
                    scriptList[existingIndex] = script;
                }
                else
                {
                    scriptList.Add(script);
                }
            }
        }

        public static void ReadTeamTypes(
            this List<TeamType> teamTypeList,
            IniFile iniFile,
            Func<string, HouseType> houseTypeFinder,
            Func<string, Script> scriptFinder,
            Func<string, TaskForce> taskForceFinder,
            Func<string, Tag> tagFinder,
            List<TeamTypeFlag> teamTypeFlags,
            Action<string> errorLogger,
            bool isGlobal)
        {
            var teamTypeListSection = iniFile.GetSection("TeamTypes");
            if (teamTypeListSection == null)
                return;

            foreach (var kvp in teamTypeListSection.Keys)
            {
                if (string.IsNullOrWhiteSpace(kvp.Key) || string.IsNullOrWhiteSpace(kvp.Value))
                    continue;

                var teamTypeSection = iniFile.GetSection(kvp.Value);
                if (teamTypeSection == null)
                    continue;

                var teamType = new TeamType(kvp.Value);
                teamType.IsGlobalTeamType = isGlobal;
                teamType.ReadPropertiesFromIniSection(teamTypeSection);
                string houseTypeIniName = teamTypeSection.GetStringValue("House", string.Empty);
                string scriptId = teamTypeSection.GetStringValue("Script", string.Empty);
                string taskForceId = teamTypeSection.GetStringValue("TaskForce", string.Empty);
                string tagId = teamTypeSection.GetStringValue("Tag", string.Empty);

                if (!Helpers.IsStringNoneValue(houseTypeIniName))
                {
                    teamType.HouseType = houseTypeFinder(houseTypeIniName);

                    if (teamType.HouseType == null)
                    {
                        errorLogger(string.Format(Translate("ListExtensions.ScriptParseError", 
                            "TeamType {0} has an invalid owner ({1}) specified!"), teamType.ININame, houseTypeIniName));
                    }
                }

                teamType.Script = scriptFinder(scriptId);
                teamType.TaskForce = taskForceFinder(taskForceId);

                if (teamType.Script == null)
                {
                    errorLogger(string.Format(Translate("ListExtensions.TeamTypes.ScriptNotFound", 
                        "TeamType {0} has an invalid script ({1}) specified!"), teamType.ININame, scriptId));
                }

                if (teamType.TaskForce == null)
                {
                    errorLogger(string.Format(Translate("ListExtensions.TeamTypes.TaskForceNotFound", 
                        "TeamType {0} has an invalid TaskForce ({1}) specified!"), teamType.ININame, taskForceId));
                }

                if (tagFinder != null && !string.IsNullOrWhiteSpace(tagId))
                {
                    teamType.Tag = tagFinder(tagId);

                    if (teamType.Tag == null)
                    {
                        errorLogger(string.Format(Translate("ListExtensions.TeamTypes.TagNotFound", 
                            "TeamType {0} has an invalid tag ({1}) specified!"), teamType.ININame, tagId));
                    }
                }

                teamTypeFlags.ForEach(ttflag =>
                {
                    if (teamTypeSection.GetBooleanValue(ttflag.Name, false))
                        teamType.EnableFlag(ttflag.Name);
                });

                var editorSectionName = iniFile.GetSection("EditorTeamTypeInfo");
                if (editorSectionName != null)
                {
                    teamType.EditorColor = editorSectionName.GetStringValue(teamType.ININame, null);
                }

                teamTypeList.Add(teamType);
            }
        }
    }
}
