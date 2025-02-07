﻿using StudioCore.Banks.AliasBank;
using StudioCore.Core.Project;
using StudioCore.Editor;
using StudioCore.Resource.Locators;
using StudioCore.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StudioCore.Caches;

public class AliasCache
{
    public List<string> CharacterList = new List<string>();
    public List<string> AssetList = new List<string>();
    public List<string> PartList = new List<string>();
    public Dictionary<string, List<string>> MapPieceDict = new Dictionary<string, List<string>>();

    public Dictionary<string, AliasReference> Characters = new Dictionary<string, AliasReference>();
    public Dictionary<string, AliasReference> Assets = new Dictionary<string, AliasReference>();
    public Dictionary<string, AliasReference> Parts = new Dictionary<string, AliasReference>();
    public Dictionary<string, AliasReference> MapPieces = new Dictionary<string, AliasReference>();

    public bool UpdateCacheComplete = false;

    public AliasCache()
    {
    }

    public void BuildCache()
    {
        UpdateCacheComplete = false;
        if (Smithbox.EditorHandler != null)
        {
            Smithbox.EditorHandler.TextureViewer.InvalidateCachedName = true;
        }

        AliasUtils.MapNameAliasCache = new Dictionary<string, string>();
        AliasUtils.CharacterNameAliasCache = new Dictionary<string, string>();
        AliasUtils.AssetNameAliasCache = new Dictionary<string, string>();
        AliasUtils.PartNameAliasCache = new Dictionary<string, string>();
        AliasUtils.MapPieceNameAliasCache = new Dictionary<string, string>();

        Characters = new Dictionary<string, AliasReference>();
        Assets = new Dictionary<string, AliasReference>();
        Parts = new Dictionary<string, AliasReference>();
        MapPieces = new Dictionary<string, AliasReference>();

        CharacterList = ResourceListLocator.GetChrModels();
        AssetList = ResourceListLocator.GetObjModels();
        PartList = ResourceListLocator.GetPartsModels();
        MapPieceDict = new Dictionary<string, List<string>>();

        if (Smithbox.BankHandler.CharacterAliases.Aliases != null)
        {
            Characters = Smithbox.BankHandler.CharacterAliases.GetEntries();
        }
        if (Smithbox.BankHandler.AssetAliases.Aliases != null)
        {
            Assets = Smithbox.BankHandler.AssetAliases.GetEntries();
        }
        if (Smithbox.BankHandler.PartAliases.Aliases != null)
        {
            Parts = Smithbox.BankHandler.PartAliases.GetEntries();
        }
        if (Smithbox.BankHandler.MapPieceAliases.Aliases != null)
        {
            MapPieces = Smithbox.BankHandler.MapPieceAliases.GetEntries();
        }

        List<string> mapList = MapLocator.GetFullMapList();

        foreach (var mapId in mapList)
        {
            var assetMapId = MapLocator.GetAssetMapID(mapId);

            List<ResourceDescriptor> modelList = new List<ResourceDescriptor>();

            if (Smithbox.ProjectType == ProjectType.DS2S || Smithbox.ProjectType == ProjectType.DS2)
            {
                modelList = ResourceListLocator.GetMapModelsFromBXF(mapId);
            }
            else
            {
                modelList = ResourceListLocator.GetMapModels(mapId);
            }

            var cache = new List<string>();
            foreach (var model in modelList)
            {
                cache.Add(model.AssetName);
            }

            if (!MapPieceDict.ContainsKey(assetMapId))
            {
                MapPieceDict.Add(assetMapId, cache);
            }
        }

        UpdateCacheComplete = true;

        TaskLogs.AddLog($"Name Cache: Loaded Asset Browser Names");
    }
}
