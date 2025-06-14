using Rampastring.Tools;
using System;
using System.IO;
using TSMapEditor.Extensions;

namespace TSMapEditor.CCEngine
{
    public class GameConfigINIFiles
    {
        public GameConfigINIFiles(string gameDirectory, CCFileManager fileManager)
        {
            RulesIni = IniFileEx.FromPathOrMix(Constants.RulesIniPath, gameDirectory, fileManager, true);
            FirestormIni = IniFileEx.FromPathOrMix(Constants.FirestormIniPath, gameDirectory, fileManager, true);

            if (RulesIni == null && FirestormIni == null)
                throw new FileNotFoundException("No Rules.ini found! (including derivates like Firestorm.ini / Rulesmd.ini)");

            if (RulesIni == null)
                RulesIni = new IniFileEx();

            if (FirestormIni == null)
                FirestormIni = new IniFileEx();

            ArtIni = IniFileEx.FromPathOrMix(Constants.ArtIniPath, gameDirectory, fileManager);
            ArtFSIni = IniFileEx.FromPathOrMix(Constants.FirestormArtIniPath, gameDirectory, fileManager);
            AIIni = IniFileEx.FromPathOrMix(Constants.AIIniPath, gameDirectory, fileManager);
            AIFSIni = IniFileEx.FromPathOrMix(Constants.FirestormAIIniPath, gameDirectory, fileManager);

            IniFile artOverridesIni = Helpers.ReadConfigINI("ArtOverrides.ini");
            IniFile.ConsolidateIniFiles(ArtFSIni, artOverridesIni);
        }

        public IniFileEx RulesIni { get; }
        public IniFileEx FirestormIni { get; }
        public IniFileEx ArtIni { get; }
        public IniFileEx ArtFSIni { get; }
        public IniFileEx AIIni { get; }
        public IniFileEx AIFSIni { get; }
    }
}
