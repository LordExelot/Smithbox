﻿using Andre.Formats;
using ImGuiNET;
using StudioCore.Core.Project;
using StudioCore.Editor;
using StudioCore.Interface;
using StudioCore.Platform;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace StudioCore.Editors.ParamEditor;

public static class ParamComparisonReport
{
    public static bool ShowReportModal = false;

    public static string CurrentParamProcessing = "";

    public static string ReportText = "";

    public static bool IsReportGenerated = false;
    public static bool IsGeneratingReport = false;

    public static bool ImportNamesOnGeneration_Primary = false;
    public static bool ImportNamesOnGeneration_Compare = false;

    public static bool ExcludeRowIndexedParams = true;
    private static List<string> ExcludedParams = new List<string>()
    {
         // ER
        "RandomAppearParam",

        // AC6
        "ThrustersParam_NPC",
        "ThrustersParam_PC"
    };

    public static void ViewReport()
    {
        ShowReportModal = true;
    }

    public static void GenerateReport()
    {
        IsReportGenerated = false;
        IsGeneratingReport = true;
        ReportText = "";

        var primaryBank = ParamBank.PrimaryBank;
        var compareBank = ParamBank.VanillaBank;

        if(CurrentCompareBankType == 1)
        {
            if (ParamBank.AuxBanks.Count <= 0)
            {
                PlatformUtils.Instance.MessageBox("No comparison bank has been loaded. Report generation ended.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error);

                IsGeneratingReport = false;
                IsReportGenerated = false;
                return;
            }
            else
            {
                var auxBank = ParamBank.AuxBanks.First().Value;
                compareBank = auxBank;
            }
        }

        if (ImportNamesOnGeneration_Primary)
        {
            Smithbox.EditorHandler.ParamEditor.EditorActionManager.ExecuteAction(
                primaryBank.LoadParamDefaultNames(null, false, false, false));
        }

        if (ImportNamesOnGeneration_Compare)
        {
            Smithbox.EditorHandler.ParamEditor.EditorActionManager.ExecuteAction(
                compareBank.LoadParamDefaultNames(null, false, false, false));
        }

        AddLog($"# Field values follow this format: [Comparison Value] -> [Primary Value]");

        foreach (var param in primaryBank.Params)
        {
            CurrentParamProcessing = param.Key;

            if (ExcludeRowIndexedParams)
            {
                if (ExcludedParams.Contains(param.Key))
                {
                    continue;  
                }
            }
            
            var primaryParam = param.Value;
            if (compareBank.Params.ContainsKey(param.Key))
            {
                var compareParam = compareBank.Params[param.Key];

                ReportDifferences(param.Key, primaryParam, compareParam);
            }
            else
            {
                AddLog($"{param.Key} does not exist in comparison.");
            }
        }

        IsGeneratingReport = false;
        IsReportGenerated = true;
    }

    public static void ReportDifferences(string paramKey, Param primaryParam, Param compareParam)
    {
        bool HadParamDifference = false;
        bool HadRowDifference = false;
        bool HadCellDifference = false;

        foreach (var primaryRow in primaryParam.Rows)
        {
            // TODO: Need to account for row index
            var compareRow = compareParam.Rows.Where(e => e.ID == primaryRow.ID).FirstOrDefault();

            if(compareRow == null)
            {
                if (!HadParamDifference)
                {
                    HadParamDifference = true;
                    AddLog($"[-- {paramKey} --]");
                }

                if (primaryRow.Name != "")
                {
                    AddLog($"  {primaryRow.ID} ({primaryRow.Name}) does not exist in comparison.");
                }
                else
                {
                    AddLog($"  {primaryRow.ID} does not exist in comparison.");
                }
            }
            else
            {
                HadRowDifference = false;
                HadCellDifference = false;

                foreach (var primaryCell in primaryRow.Cells)
                {
                    var compareCell = compareRow.Cells.Where(e => e.Def == primaryCell.Def).FirstOrDefault();

                    if (!compareCell.IsNull())
                    {
                        var primaryValue = primaryCell.Value.ToString();
                        var compareValue = compareCell.Value.ToString();

                        if (primaryValue != compareValue)
                        {
                            if (!HadParamDifference)
                            {
                                HadParamDifference = true;
                                AddLog($"[-- {paramKey} --]");
                            }

                            if (!HadRowDifference)
                            {
                                HadRowDifference = true;
                                if (primaryRow.Name != "")
                                {
                                    AddLog($"  {primaryRow.ID} {primaryRow.Name}:");
                                }
                                else
                                {
                                    AddLog($"  {primaryRow.ID}:");
                                }
                            }

                            HadCellDifference = true;

                            AddLog($"    [{primaryCell.Def.InternalName}]: {compareValue} -> {primaryValue}");
                        }
                    }
                }

                if (HadCellDifference)
                {
                    AddLog($"");
                }
            }
        }
    }

    public static void AddLog(string text)
    {
        ReportText = $"{ReportText}{text}\n";
    }

    public static int CurrentCompareBankType = 0;

    public static string[] CompareBankType =
    {
        "Vanilla Bank",
        "Aux Bank"
    };

    public static void Display()
    {
        var textPaneSize = new Vector2(UI.Current.Interface_ModalWidth, UI.Current.Interface_ModalHeight);

        UIHelper.WrappedTextColored(UI.Current.ImGui_AliasName_Text, "Comparison Report");
        ImGui.Separator();
        UIHelper.WrappedText($"Primary Bank - Param Version: {ParamBank.PrimaryBank.ParamVersion}");

        if (CurrentCompareBankType == 0)
        {
            UIHelper.WrappedText($"Comparison Bank - Param Version: {ParamBank.VanillaBank.ParamVersion}");
        }
        else if (CurrentCompareBankType == 1)
        {
            if (ParamBank.AuxBanks.Count > 0)
            {
                var auxBank = ParamBank.AuxBanks.First().Value;
                UIHelper.WrappedText($"Comparison Bank - Param Version: {auxBank.ParamVersion}");
            }
            else
            {
                UIHelper.WrappedText($"Comparison Bank - No Comparison Bank loaded.");
            }
        }
        ImGui.Separator();
        UIHelper.WrappedTextColored(UI.Current.ImGui_AliasName_Text, "Comparison Bank Type");
        if (ImGui.Combo("##compareBankTarget", ref CurrentCompareBankType, CompareBankType, CompareBankType.Length))
        {
        }
        ImGui.Checkbox("Import Row Names on Report Generation for Primary Bank", ref ImportNamesOnGeneration_Primary);
        ImGui.Checkbox("Import Row Names on Report Generation for Comparison Bank", ref ImportNamesOnGeneration_Compare);
        ImGui.Checkbox("Ignore Row Indexed Params", ref ExcludeRowIndexedParams);
        UIHelper.ShowHoverTooltip("Ignores params that have multiple rows of the same ID (e.g. use row index rather than ID for identity), as this isn't handled currently and will erroneously show differences.");

        ImGui.Separator();

        if (IsReportGenerated)
        {
            var buttonSize = new Vector2(UI.Current.Interface_ModalWidth / 3, UI.Current.Interface_ButtonHeight);

            ImGui.InputTextMultiline("##reportText", ref ReportText, uint.MaxValue, textPaneSize, ImGuiInputTextFlags.ReadOnly);

            if (ImGui.Button("Re-generate", buttonSize))
            {
                TaskManager.Run(
                    new TaskManager.LiveTask($"Generate Param Difference Report", TaskManager.RequeueType.None, false,
                () =>
                {
                    GenerateReport();
                }));
            }
            ImGui.SameLine();
            if (ImGui.Button("Copy", buttonSize))
            {
                UIHelper.CopyToClipboard(ReportText);
            }
            ImGui.SameLine();
            if (ImGui.Button("Close", buttonSize))
            {
                ShowReportModal = false;
            }
        }
        else if(IsGeneratingReport)
        {
            var buttonSize = new Vector2(UI.Current.Interface_ModalWidth, UI.Current.Interface_ButtonHeight);

            ImGui.Text("Report is being generated...");
            ImGui.Text($"Current Param: {CurrentParamProcessing}");

            if (ImGui.Button("Close", buttonSize))
            {
                ShowReportModal = false;
            }
        }
        else
        {
            var buttonSize = new Vector2(UI.Current.Interface_ModalWidth / 2, UI.Current.Interface_ButtonHeight);

            if (ImGui.Button("Generate", buttonSize))
            {
                TaskManager.Run(
                    new TaskManager.LiveTask($"Generate Param Difference Report", TaskManager.RequeueType.None, false,
                () =>
                {
                    GenerateReport();
                }));
            }
            ImGui.SameLine();
            if (ImGui.Button("Close", buttonSize))
            {
                ShowReportModal = false;
            }
        }
    }

    public static void HandleReportModal()
    {
        if (ShowReportModal)
        {
            ImGui.OpenPopup("Param Comparison Report");
        }

        if (ImGui.BeginPopupModal("Param Comparison Report", ref ShowReportModal, ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.AlwaysAutoResize))
        {
            Display();

            ImGui.EndPopup();
        }
    }
}
