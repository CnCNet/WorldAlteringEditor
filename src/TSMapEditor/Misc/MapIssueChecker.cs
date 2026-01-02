using Rampastring.Tools;
using System;
using System.Collections.Generic;
using TSMapEditor.CCEngine;
using TSMapEditor.Models;
using TSMapEditor.Models.Enums;
using TSMapEditor.Rendering;

namespace TSMapEditor.Misc
{
    public static class MapIssueChecker
    {
        private const int EnableTriggerActionIndex = 53;
        private const int TriggerParamIndex = 1;

        /// <summary>
        /// Checks the map for issues.
        /// Returns a list of issues found.
        /// TODO refactor, split each check to its own function
        /// </summary>
        public static List<string> CheckForIssues(Map map)
        {
            var issueList = new List<string>();

            map.DoForAllValidTiles(cell =>
            {
                if (cell.HasTiberium())
                {
                    ITileImage tile = map.TheaterInstance.GetTile(cell.TileIndex);
                    ISubTileImage subTile = tile.GetSubTile(cell.SubTileIndex);

                    // Check whether the cell has tiberium on an impassable terrain type
                    if (Helpers.IsLandTypeImpassable(subTile.TmpImage.TerrainType, true))
                    {
                        issueList.Add(string.Format(Translate(map, "CheckForIssues.ImpassableTile",
                            "Cell at {0} has Tiberium on an otherwise impassable cell. This can cause harvesters to get stuck."),
                                cell.CoordsToPoint()));
                    }

                    // Check for tiberium on ramps that don't support tiberium on them
                    if (subTile.TmpImage.RampType > RampType.South)
                    {
                        issueList.Add(string.Format(Translate(map, "CheckForIssues.TiberiumUnsupportedRamp",
                            "Cell at {0} has Tiberium on a ramp that does not allow Tiberium on it. This can crash the game!"),
                                cell.CoordsToPoint()));
                    }
                }
            });

            // Check for multiple houses having the same ININame
            for (int i = 0; i < map.Houses.Count; i++)
            {
                House duplicate = map.Houses.Find(h => h != map.Houses[i] && h.ININame == map.Houses[i].ININame);
                if (duplicate != null)
                {
                    issueList.Add(string.Format(Translate(map, "CheckForIssues.DuplicateHouseININame",
                        "The map has multiple houses named \"{0}\"! This will result in a corrupted house list in-game!"), duplicate.ININame));
                    break;
                }
            }

            // Check for teamtypes having no taskforce or script
            map.TeamTypes.ForEach(tt =>
            {
                if (tt.TaskForce == null)
                    issueList.Add(string.Format(Translate(map, "CheckForIssues.TeamTypeWithoutTaskForce",
                        "TeamType \"{0}\" has no TaskForce set!"), tt.Name));

                if (tt.Script == null)
                    issueList.Add(string.Format(Translate(map, "CheckForIssues.TeamTypeWithoutScript",
                        "TeamType \"{0}\" has no Script set!"), tt.Name));
            });

            const int DisableTriggerActionIndex = 54;

            // Check for triggers that are disabled and are never enabled by any other triggers
            map.Triggers.ForEach(trigger =>
            {
                if (!trigger.Disabled)
                    return;

                const int RevealAllMapActionIndex = 16;

                // If this trigger has a "reveal all map" action, don't create an issue - those are usually only for debugging
                if (trigger.Actions.Exists(a => a.ActionIndex == RevealAllMapActionIndex))
                    return;

                // Allow the user to skip this warning by including "DEBUG" or "OBSOLETE" in the trigger's name
                if (trigger.Name.ToUpperInvariant().Contains("DEBUG") || trigger.Name.ToUpperInvariant().Contains("OBSOLETE"))
                    return;

                // Is this trigger enabled by another trigger?
                if (map.Triggers.Exists(otherTrigger => otherTrigger != trigger && otherTrigger.Actions.Exists(a => a.ActionIndex == EnableTriggerActionIndex && a.Parameters[TriggerParamIndex] == trigger.ID)))
                    return;

                // If it's not enabled by another trigger, add an issue
                issueList.Add(string.Format(Translate(map, "CheckForIssues.TriggerDisabled",
                    "Trigger \"{0}\" ({1}) is disabled and never enabled by another trigger." + Environment.NewLine +
                    "Did you forget to enable it? If the trigger exists for debugging purposes, add DEBUG or OBSOLETE to its name to skip this warning."),
                        trigger.Name, trigger.ID));
            });

            // Check for triggers that are enabled by other triggers, but never disabled - enabling them is
            // useless in that case and likely means that there's a scripting issue
            map.Triggers.ForEach(trigger =>
            {
                if (trigger.Disabled)
                    return;

                // If this trigger is never enabled by another trigger, skip
                if (!map.Triggers.Exists(otherTrigger => otherTrigger != trigger && otherTrigger.Actions.Exists(a => a.ActionIndex == EnableTriggerActionIndex && a.Parameters[TriggerParamIndex] == trigger.ID)))
                    return;

                // If this trigger is disabled by itself or some other trigger, skip
                if (map.Triggers.Exists(otherTrigger => otherTrigger.Actions.Exists(a => a.ActionIndex == DisableTriggerActionIndex && a.Parameters[TriggerParamIndex] == trigger.ID)))
                    return;

                // This trigger is never disabled, but it is enabled by at least 1 other trigger - add an issue
                issueList.Add(string.Format(Translate(map, "CheckForIssues.TriggerEnabledByOtherTriggers",
                    "Trigger \"{0}\" ({1}) is enabled by another trigger, but it is never in a disabled state" + Environment.NewLine +
                    "(it is neither disabled by default nor disabled by other triggers). Did you forget to disable it?"),
                        trigger.Name, trigger.ID));
            });

            // Check for triggers that enable themselves, there's no need to ever do this -> either redundant action or a scripting error
            map.Triggers.ForEach(trigger =>
            {
                if (!trigger.Actions.Exists(a => a.ActionIndex == EnableTriggerActionIndex && a.Parameters[TriggerParamIndex] == trigger.ID))
                    return;

                issueList.Add(string.Format(Translate(map, "CheckForIssues.TriggerEnableSelf",
                    "Trigger \"{0}\" ({1}) has an action for enabling itself. Is it supposed to enable something else instead?"),
                        trigger.Name, trigger.ID));
            });

            // Check that the primary player house has "Player Control" enabled in case [Basic] Player= is specified
            // (iow. this is a singleplayer mission)
            if (!string.IsNullOrWhiteSpace(map.Basic.Player) && !Helpers.IsStringNoneValue(map.Basic.Player))
            {
                House matchingHouse = map.GetHouses().Find(h => h.ININame == map.Basic.Player);
                if (matchingHouse == null)
                    issueList.Add(Translate(map, "CheckForIssues.PlayerHouseNotFound",
                        "A nonexistent house has been specified in [Basic] Player= ."));
                else if (!matchingHouse.PlayerControl)
                    issueList.Add(Translate(map, "CheckForIssues.PlayerHouseNotPlayerControlled",
                        "The human player's house does not have the \"Player-Controlled\" flag checked."));
            }

            // Check for more than 127 tunnel tubes
            if (map.Tubes.Count > Map.MaxTubes)
            {
                issueList.Add(string.Format(Translate(map, "CheckForIssues.MaxTubesExceeded",
                    "The map has more than {0} tunnel tubes. This might cause issues when units cross the tunnels."), Map.MaxTubes));
            }

            // Check for vehicles sharing the same follows index and for vehicles following themselves
            List<Unit> followedUnits = new List<Unit>();
            for (int i = 0; i < map.Units.Count; i++)
            {
                var unit = map.Units[i];
                int followsId = unit.FollowerUnit == null ? -1 : map.Units.IndexOf(unit.FollowerUnit);
                if (followsId == -1)
                    continue;

                if (followedUnits.Contains(unit.FollowerUnit))
                {
                    issueList.Add(string.Format(Translate(map, "CheckForIssues.UnitFollowsMultipleUnits",
                        "Multiple units are configured to make unit {0} at {1} to follow them! " + Environment.NewLine +
                            "This can cause strange behaviour in the game. {2} at {3} is one of the followed units."),
                            unit.FollowerUnit.UnitType.ININame, unit.FollowerUnit.Position, unit.UnitType.ININame, unit.Position));
                }
                else
                {
                    followedUnits.Add(unit.FollowerUnit);
                }

                if (followsId < -1)
                {
                    issueList.Add(string.Format(Translate(map, "CheckForIssues.NegativeFollowerID",
                        "Unit {0} at {1} has a follower ID below -1. It is unknown how the game reacts to this."),
                        unit.UnitType.ININame, 1));
                }
                else if (followsId == i)
                {
                    issueList.Add(string.Format(Translate(map, "CheckForIssues.UnitFollowSelf",
                        "Unit {0} at {1} follows itself! This can cause the game to crash or freeze!"),
                        unit.UnitType.ININame, unit.Position));
                }
            }

            var reportedTeams = new List<TeamType>();

            // Check for AI Trigger linked TeamTypes having Max=0
            foreach (var aiTrigger in map.AITriggerTypes)
            {
                CheckForAITriggerTeamWithMaxZeroIssue(map, aiTrigger, aiTrigger.PrimaryTeam, reportedTeams, issueList);
                CheckForAITriggerTeamWithMaxZeroIssue(map, aiTrigger, aiTrigger.SecondaryTeam, reportedTeams, issueList);
            }

            // Check for triggers having 0 events or actions
            foreach (var trigger in map.Triggers)
            {
                if (trigger.Conditions.Count == 0)
                {
                    issueList.Add(string.Format(Translate(map, "CheckForIssues.NoTriggerConditions",
                        "Trigger '{0}' has 0 events specified. It will never be fired. Did you forget to give it events?"), trigger.Name));
                }

                if (trigger.Actions.Count == 0)
                {
                    issueList.Add(string.Format(Translate(map, "CheckForIssues.NoTriggerActions",
                        "Trigger '{0}' has 0 actions specified. It will not do anything. Did you forget to give it actions?"), trigger.Name));
                }
            }

            // Check for triggers using the "Entered by" event without being attached to anything
            const int EnteredByConditionIndex = 1;
            foreach (var trigger in map.Triggers)
            {
                if (!trigger.Conditions.Exists(c => c.ConditionIndex == EnteredByConditionIndex))
                    continue;

                var tag = map.Tags.Find(t => t.Trigger == trigger);
                if (tag == null)
                    continue;

                if (!map.Structures.Exists(s => s.AttachedTag == tag) &&
                    !map.Infantry.Exists(i => i.AttachedTag == tag) &&
                    !map.Units.Exists(u => u.AttachedTag == tag) &&
                    !map.Aircraft.Exists(a => a.AttachedTag == tag) &&
                    !map.TeamTypes.Exists(tt => tt.Tag == tag) &&
                    !map.CellTags.Exists(ct => ct.Tag == tag) &&
                    !map.Triggers.Exists(otherTrigger => otherTrigger.LinkedTrigger == trigger))
                {
                    issueList.Add(string.Format(Translate(map, "CheckForIssues.TriggerEnteredByNoObjects",
                        "Trigger '{0}' is using the \"Entered by...\" event without being attached to any object, cell, or team. Did you forget to attach it?"),
                            trigger.Name));
                }
            }

            // Check for triggers using the "Bridge destroyed" event without being linked to any cell
            const int BridgeDestroyedConditionIndex = 31;
            foreach (var trigger in map.Triggers)
            {
                if (!trigger.Conditions.Exists(c => c.ConditionIndex == BridgeDestroyedConditionIndex))
                    continue;

                var tag = map.Tags.Find(t => t.Trigger == trigger);
                if (tag == null)
                    continue;

                if (!map.CellTags.Exists(ct => ct.Tag == tag))
                {
                    issueList.Add(string.Format(Translate(map, "CheckForIssues.TriggerBridgeDestroyedNoCellTags",
                        "Trigger '{0}' is using the \"Bridge destroyed\" event, but it is not attached to any CellTag. Did you forget to place a celltag for it?"),
                            trigger.Name));
                }
            }

            // Check for triggers using an object-specific event (like "destroyed" or "damaged") without
            // being linked to any object
            var objectSpecificEventIndexes = new List<int>() {
                4,  // Discovered by player
                6,  // Attacked by any house
                7,  // Destroyed by any house
                29, // Destroyed by anything (not infiltrate)
                33, // Selected by player
                34, // Comes near waypoint
                38, // First damaged (combat only)
                39, // Half health (combat only)
                40, // Quarter health (combat only)
                41, // First damaged (any source)
                42, // Half health (any source)
                43, // Quarter health (any source)
                44, // Attacked by (house)
                48  // Destroyed by anything
            };

            if (!Constants.IsRA2YR)
                objectSpecificEventIndexes.Add(55); // Limpet attached - Firestorm only, not in RA2/YR

            foreach (var trigger in map.Triggers)
            {
                int indexInList = objectSpecificEventIndexes.FindIndex(eventIndex => trigger.Conditions.Exists(c => c.ConditionIndex == eventIndex));
                if (indexInList == -1)
                    continue;

                int usedEventIndex = objectSpecificEventIndexes[indexInList];
                var triggerEventType = map.EditorConfig.TriggerEventTypes.GetValueOrDefault(usedEventIndex);

                if (triggerEventType == null)
                    continue;

                var tag = map.Tags.Find(t => t.Trigger == trigger);
                if (tag == null)
                    continue;

                if (!map.Structures.Exists(s => s.AttachedTag == tag) &&
                    !map.Infantry.Exists(i => i.AttachedTag == tag) &&
                    !map.Units.Exists(u => u.AttachedTag == tag) &&
                    !map.Aircraft.Exists(a => a.AttachedTag == tag) &&
                    !map.TeamTypes.Exists(tt => tt.Tag == tag) &&
                    !map.Triggers.Exists(otherTrigger => otherTrigger.LinkedTrigger == trigger))
                {
                    string eventName = triggerEventType.Name;

                    issueList.Add(string.Format(Translate(map, "CheckForIssues.TriggerEventNoObjectAttached",
                        "Trigger '{0}' is using the {1} event without being attached to any object or team. Did you forget to attach it?"),
                            trigger.Name, eventName));
                }
            }

            // Check for triggers being attached to themselves (potentially recursively)
            foreach (var trigger in map.Triggers)
            {
                if (trigger.LinkedTrigger == null)
                    continue;

                Trigger linkedTrigger = trigger.LinkedTrigger;
                while (linkedTrigger != null)
                {
                    if (linkedTrigger == trigger)
                    {
                        issueList.Add(string.Format(Translate(map, "CheckForIssues.TriggerAttachedToSelf",
                            "Trigger '{0}' is attached to itself (potentially through other triggers). This will cause the game to crash!"), trigger.Name));
                        break;
                    }

                    linkedTrigger = linkedTrigger.LinkedTrigger;
                }
            }

            // Check for invalid TeamType parameter values in triggers
            foreach (var trigger in map.Triggers)
            {
                foreach (var action in trigger.Actions)
                {
                    if (!map.EditorConfig.TriggerActionTypes.TryGetValue(action.ActionIndex, out var triggerActionType))
                        continue;

                    for (int i = 0; i < triggerActionType.Parameters.Length; i++)
                    {
                        if (triggerActionType.Parameters[i].TriggerParamType == TriggerParamType.TeamType)
                        {
                            if (!map.TeamTypes.Exists(tt => tt.ININame == action.Parameters[i]) && !map.Rules.TeamTypes.Exists(tt => tt.ININame == action.Parameters[i]))
                            {
                                issueList.Add(string.Format(Translate(map, "CheckForIssues.InvalidTriggerActionTeamType",
                                    "Trigger '{0}' has a nonexistent TeamType specified as a parameter for one or more of its actions."), trigger.Name));
                                break;
                            }
                        }
                    }
                }
            }

            var houseTypes = map.GetHouseTypes();

            // Check for invalid HouseType parameter values in triggers
            foreach (var trigger in map.Triggers)
            {
                foreach (var action in trigger.Actions)
                {
                    if (!map.EditorConfig.TriggerActionTypes.TryGetValue(action.ActionIndex, out var triggerActionType))
                        continue;

                    for (int i = 0; i < triggerActionType.Parameters.Length; i++)
                    {
                        if (triggerActionType.Parameters[i].TriggerParamType == TriggerParamType.HouseType)
                        {
                            int paramAsInt = Conversions.IntFromString(action.Parameters[i], -1);

                            if (!houseTypes.Exists(ht => ht.Index == paramAsInt))
                            {
                                issueList.Add(string.Format(Translate(map, "CheckForIssues.InvalidTriggerActionHouse",
                                    "Trigger '{0}' has a nonexistent HouseType specified as a parameter for one or more of its actions."), trigger.Name));
                                break;
                            }
                        }
                    }
                }
            }

            // Check for triggers having invalid owners
            foreach (var trigger in map.Triggers)
            {
                if (!houseTypes.Exists(ht => trigger.HouseType == ht.ININame))
                    issueList.Add(string.Format(Translate(map, "CheckForIssues.InvalidTriggerOwner",
                        "Trigger '{0}' has a nonexistent HouseType '{1}' specified as its owner."),
                            trigger.Name, trigger.HouseType));
            }

            // Check for triggers having too many actions. This can cause a crash because the game's buffer for parsing trigger actions
            // is limited (to 512 chars according to ModEnc)
            if (Constants.WarnOfTooManyTriggerActions)
            {
                const int maxActionCount = 18;
                foreach (var trigger in map.Triggers)
                {
                    if (trigger.Actions.Count > maxActionCount)
                    {
                        issueList.Add(string.Format(Translate(map, "CheckForIssues.TriggerTooManyActions",
                            "Trigger '{0}' has more than {1} actions! This can cause the game to crash! Consider splitting it up to multiple triggers."),
                                trigger.Name, maxActionCount));
                    }
                }
            }

            // In Tiberian Sun, waypoint #100 should be reserved for special dynamic use cases like paradrops
            // (it is defined as WAYPT_SPECIAL in original game code)
            if (!Constants.IsRA2YR && map.Waypoints.Exists(wp => wp.Identifier == Constants.TS_WAYPT_SPECIAL))
            {
                issueList.Add(string.Format(Translate(map, "CheckForIssues.SpecialWaypointUsed",
                    "The map makes use of waypoint #{0}. In Tiberian Sun, this waypoint is reserved for special use cases (WAYPT_SPECIAL). Using it as a normal waypoint may cause issues as it may be dynamically moved by game events."),
                        Constants.TS_WAYPT_SPECIAL));
            }

            // Check for scripts that have more than 50 Script Actions. This is unsupported by the game.
            const int maxScriptActionCount = 50;
            foreach (var script in map.Scripts)
            {
                if (script.Actions.Count > maxScriptActionCount)
                {
                    issueList.Add(string.Format(Translate(map, "CheckForIssues.ScriptTooManyActions",
                            "Script '{0}' has more than {1} actions, which is not supported by the game. Consider organizing your script actions or splitting it to multiple scripts."),
                                script.Name, maxScriptActionCount));
                }
            }

            CheckForMismatchedDifficultyEnableIssue(map, issueList);

            return issueList;
        }

        private static void CheckForAITriggerTeamWithMaxZeroIssue(Map map, AITriggerType aiTrigger, TeamType team, List<TeamType> reportedTeams, List<string> issueList)
        {
            if (reportedTeams.Contains(team))
                return;

            if (team == null)
                return;

            if (map.TeamTypes.Contains(team) && team.Max == 0)
            {
                issueList.Add(string.Format(Translate(map, "CheckForAITriggerTeamWithMaxZeroIssue.TeamTypeMax",
                    "Team '{0}', linked to AITrigger '{1}', has Max=0. This prevents the AI from building the team."),
                        team.Name, aiTrigger.Name));
                reportedTeams.Add(team);
            }
        }

        private static Difficulty GetTriggerDifficulty(Map map, Trigger trigger)
        {
            if (trigger.Hard && !trigger.Normal && !trigger.Easy)
                return Difficulty.Hard;

            if (trigger.Normal && !trigger.Hard && !trigger.Easy)
                return Difficulty.Medium;

            if (trigger.Easy && !trigger.Hard && !trigger.Normal)
                return Difficulty.Easy;

            int hardDiffGlobalVariableIndex = map.Rules.GlobalVariables.FindIndex(gv => gv.Name == "Difficulty Hard");
            int mediumDiffGlobalVariableIndex = map.Rules.GlobalVariables.FindIndex(gv => gv.Name == "Difficulty Medium");
            int easyDiffGlobalVariableIndex = map.Rules.GlobalVariables.FindIndex(gv => gv.Name == "Difficulty Easy");

            // Go through used events to figure out the trigger's difficulty
            // in case the trigger's difficulty is defined with globals (multiplayer, or older TS engine missions).
            if (hardDiffGlobalVariableIndex > -1 && mediumDiffGlobalVariableIndex > -1 && easyDiffGlobalVariableIndex > -1)
            {
                for (int i = 0; i < trigger.Conditions.Count; i++)
                {
                    const int GlobalSetConditionIndex = 27;
                    TriggerCondition condition = trigger.Conditions[i];

                    if (condition.ConditionIndex == GlobalSetConditionIndex &&
                        map.EditorConfig.TriggerEventTypes[condition.ConditionIndex].Parameters[1].TriggerParamType == TriggerParamType.GlobalVariable)
                    {
                        int paramValue = Conversions.IntFromString(condition.Parameters[1], 0);

                        if (paramValue == hardDiffGlobalVariableIndex)
                            return Difficulty.Hard;
                        else if (paramValue == mediumDiffGlobalVariableIndex)
                            return Difficulty.Medium;
                        else if (paramValue == easyDiffGlobalVariableIndex)
                            return Difficulty.Easy;
                    }
                }
            }

            return Difficulty.None;
        }

        private static void CheckForMismatchedDifficultyEnableIssue(Map map, List<string> issueList)
        {
            foreach (var trigger in map.Triggers)
            {
                Difficulty difficulty = GetTriggerDifficulty(map, trigger);

                if (difficulty == Difficulty.None)
                    continue;

                // Check if this trigger enables a trigger that does not match its difficulty level.
                // If yes, report that as a bug.
                for (int i = 0; i < trigger.Actions.Count; i++)
                {
                    TriggerAction action = trigger.Actions[i];

                    if (action.ActionIndex == EnableTriggerActionIndex)
                    {
                        var otherTrigger = map.Triggers.Find(t => t.ID == action.Parameters[TriggerParamIndex]);
                        if (otherTrigger != null)
                        {
                            Difficulty otherTriggerDifficulty = GetTriggerDifficulty(map, otherTrigger);
                            if (otherTriggerDifficulty != Difficulty.None && difficulty != otherTriggerDifficulty)
                            {
                                issueList.Add(string.Format(Translate(map, "CheckForIssues.MismatchedDifficultyForEnableTrigger",
                                    "The trigger \"{0}\" has \"{1}\" as its difficulty level, but it enables a trigger \"{2}\" which has \"{3}\" as its difficulty."),
                                    trigger.Name, Helpers.DifficultyToTranslatedString(difficulty), otherTrigger.Name, Helpers.DifficultyToTranslatedString(otherTriggerDifficulty)));
                            }
                        }
                    }
                }
            }
        }
    }
}
