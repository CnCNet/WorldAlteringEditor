﻿using System.IO;
using System.Text;
using Rampastring.Tools;

namespace TSMapEditor.Extensions;

/// <summary>
/// IniFile with support for Ares #include and Phobos $Include and $Inherits.
/// </summary>
public class IniFileEx: IniFile
{
    private static readonly string AresIncludeSection = "#include";
    private static readonly string PhobosIncludeSection = "$Include";
    private static readonly string PhobosInheritsSection = "$Inherits";

    public IniFileEx() : base() { }

    public IniFileEx(string filePath) : base(filePath)
    {
        Include();
        Inherit();
    }

    public IniFileEx(string filePath, Encoding encoding) : base(filePath, encoding)
    {
        Include();
        Inherit();
    }

    public IniFileEx(Stream stream) : base(stream)
    {
        Include();
        Inherit();
    }

    public IniFileEx(Stream stream, Encoding encoding) : base(stream, encoding)
    {
        Include();
        Inherit();
    }

    /// <summary>
    /// Includes all base INI files into this file.
    /// </summary>
    public void Include()
    {
        if (!Constants.EnableIniInclude)
            return;

        string sectionName;
        if (SectionExists(PhobosIncludeSection))
            sectionName = PhobosIncludeSection;
        else if (SectionExists(AresIncludeSection))
            sectionName = AresIncludeSection;
        else
            return;

        foreach (var pair in GetSection(sectionName).Keys)
        {
            string path = SafePath.CombineFilePath(SafePath.GetFileDirectoryName(FileName), pair.Value);
            IniFileEx includedIni = new(path);
            ConsolidateIniFiles(includedIni, this);
            Sections = includedIni.Sections;
        }
    }

    /// <summary>
    /// For each section, inherits all missing keys from listed parents.
    /// </summary>
    public void Inherit()
    {
        if (!Constants.EnableIniInheritance)
            return;

        foreach (var section in Sections)
        {
            if (!section.KeyExists(PhobosInheritsSection))
                continue;

            foreach (var parentName in section.GetListValue<string>(PhobosInheritsSection, ',', x => x))
            {
                var parent = Sections.Find(s => s.SectionName == parentName);
                if (parent == null)
                    continue;

                foreach (var pair in parent.Keys)
                {
                    if (!section.KeyExists(pair.Key))
                        section.AddKey(pair.Key, pair.Value);
                }
            }
        }
    }
}
