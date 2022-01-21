// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Designers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using ModKit;
using ModKit.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using ToyBox.classes.Infrastructure;
using UnityEngine;
using Alignment = Kingmaker.Enums.Alignment;

namespace ToyBox {
    public class PartyEditor {
        public static Settings settings => Main.settings;

        private enum ToggleChoice {
            Classes,
            Stats,
            Facts,
            Features,
            Buffs,
            Abilities,
            Spells,
            None,
        };

        private static ToggleChoice selectedToggle = ToggleChoice.None;
        private static int selectedCharacterIndex = 0;
        private static UnitEntityData charToAdd = null;
        private static UnitEntityData charToRecruit = null;
        private static UnitEntityData charToRemove = null;
        private static bool editMultiClass = false;
        private static UnitEntityData multiclassEditCharacter = null;
        private static int respecableCount = 0;
        private static int recruitableCount = 0;
        private static int selectedSpellbook = 0;
        private static (string, string) nameEditState = (null, null);
        public static int selectedSpellbookLevel = 0;
        private static bool editSpellbooks = false;
        private static UnitEntityData spellbookEditCharacter = null;
        private static readonly Dictionary<string, int> statEditorStorage = new();
        public static Dictionary<string, Spellbook> SelectedSpellbook = new();

        public static List<UnitEntityData> GetCharacterList() {
            var partyFilterChoices = CharacterPicker.GetPartyFilterChoices();
            if (partyFilterChoices == null) { return null; }
            return partyFilterChoices[Main.settings.selectedPartyFilter].func();
        }

        private static UnitEntityData GetSelectedCharacter() {
            var characterList = GetCharacterList();
            if (characterList == null || characterList.Count == 0) return null;
            if (selectedCharacterIndex >= characterList.Count) selectedCharacterIndex = 0;
            return characterList[selectedCharacterIndex];
        }
        public static void ResetGUI() {
            selectedCharacterIndex = 0;
            selectedSpellbook = 0;
            selectedSpellbookLevel = 0;
            CharacterPicker.partyFilterChoices = null;
            Main.settings.selectedPartyFilter = 0;
        }

        // This bit of kludge is added in order to tell whether our generic actions are being accessed from this screen or the Search n' Pick
        public static bool IsOnPartyEditor() => Main.settings.selectedTab == 2;

        public static void ActionsGUI(UnitEntityData ch) {
            var player = Game.Instance.Player;
            UI.Space(25);
            if (!player.PartyAndPets.Contains(ch) && player.AllCharacters.Contains(ch)) {
                UI.ActionButton("Add", () => { charToAdd = ch; }, UI.Width(150));
                UI.Space(25);
            }
            else if (player.ActiveCompanions.Contains(ch)) {
                UI.ActionButton("Remove", () => { charToRemove = ch; }, UI.Width(150));
                UI.Space(25);
            }
            else if (!player.AllCharacters.Contains(ch)) {
                recruitableCount++;
                UI.ActionButton("Recruit".cyan(), () => { charToRecruit = ch; }, UI.Width(150));
                UI.Space(25);
            }
            else {
                UI.Space(178);
            }
            if (RespecHelper.GetRespecableUnits().Contains(ch)) {
                respecableCount++;
                UI.ActionButton("Respec".cyan(), () => { Actions.ToggleModWindow(); RespecHelper.Respec(ch); }, UI.Width(150));
            }
            else {
                UI.Space(153);
            }
#if DEBUG
            UI.Space(25);
            UI.ActionButton("Log Caster Info", () => CasterHelpers.GetOriginalCasterLevel(ch.Descriptor),
                UI.AutoWidth());
#endif
        }
        public static void OnGUI() {
            var player = Game.Instance.Player;
            var filterChoices = CharacterPicker.GetPartyFilterChoices();
            if (filterChoices == null) { return; }

            charToAdd = null;
            charToRecruit = null;
            charToRemove = null;
            var characterListFunc = UI.TypePicker<List<UnitEntityData>>(
                null,
                ref Main.settings.selectedPartyFilter,
                filterChoices
                );
            var characterList = characterListFunc.func();
            var mainChar = GameHelper.GetPlayerCharacter();
            if (characterListFunc.name == "Nearby") {
                UI.Slider("Nearby Distance", ref CharacterPicker.nearbyRange, 1f, 200, 25, 0, " meters", UI.Width(250));
                characterList = characterList.OrderBy((ch) => ch.DistanceTo(mainChar)).ToList();
            }
            UI.Space(20);
            var chIndex = 0;
            recruitableCount = 0;
            respecableCount = 0;
            var selectedCharacter = GetSelectedCharacter();
            var isWide = UI.IsWide;
            if (Main.IsInGame) {
                using (UI.HorizontalScope()) {
                    UI.Label($"Party Level ".cyan() + $"{Game.Instance.Player.PartyLevel}".orange().bold(), UI.AutoWidth());
                    UI.Space(25);
#if false   // disabled until we fix performance
                    var encounterCR = CheatsCombat.GetEncounterCr();
                    if (encounterCR > 0) {
                        UI.Label($"Encounter CR ".cyan() + $"{encounterCR}".orange().bold(), UI.AutoWidth());
                    }
#endif
                }
            }
            foreach (var ch in characterList) {
                var classData = ch.Progression.Classes;
                // TODO - understand the difference between ch.Progression and ch.Descriptor.Progression
                var progression = ch.Descriptor.Progression;
                var xpTable = progression.ExperienceTable;
                var level = progression.CharacterLevel;
                var mythicLevel = progression.MythicLevel;
                var spellbooks = ch.Spellbooks;
                var spellCount = spellbooks.Sum((sb) => sb.GetAllKnownSpells().Count());
                var isOnTeam = player.AllCharacters.Contains(ch);
                using (UI.HorizontalScope()) {
                    var name = ch.CharacterName;
                    if (Game.Instance.Player.AllCharacters.Contains(ch)) {
                        if (isWide) {
                            if (UI.EditableLabel(ref name, ref nameEditState, 200, n => n.orange().bold(), UI.MinWidth(100), UI.MaxWidth(600))) {
                                ch.Descriptor.CustomName = name;
                                Main.SetNeedsResetGameUI();
                            }
                        }
                        else
                            if (UI.EditableLabel(ref name, ref nameEditState, 200, n => n.orange().bold(), UI.Width(230))) {
                            ch.Descriptor.CustomName = name;
                            Main.SetNeedsResetGameUI();
                        }
                    }
                    else {
                        if (isWide)
                            UI.Label(ch.CharacterName.orange().bold(), UI.MinWidth(100), UI.MaxWidth(600));
                        else
                            UI.Label(ch.CharacterName.orange().bold(), UI.Width(230));
                    }
                    UI.Space(5);
                    var distance = mainChar.DistanceTo(ch); ;
                    UI.Label(distance < 1 ? "" : distance.ToString("0") + "m", UI.Width(75));
                    UI.Space(5);
                    int nextLevel;
                    for (nextLevel = level;
                        nextLevel + 1 < xpTable.Bonuses.Length && progression.Experience >= xpTable.GetBonus(nextLevel + 1) && xpTable.HasBonusForLevel(nextLevel + 1); 
                        nextLevel++) { }
                    if (nextLevel <= level || !isOnTeam)
                        UI.Label((level < 10 ? "   lvl" : "   lv").green() + $" {level}", UI.Width(90));
                    else
                        UI.Label((level < 10 ? "  " : "") + $"{level} > " + $"{nextLevel}".cyan(), UI.Width(90));
                    // Level up code adapted from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/2
                    if (player.AllCharacters.Contains(ch)) {
                        if (nextLevel + 1 < xpTable.Bonuses.Length && xpTable.HasBonusForLevel(nextLevel + 1)) {
                            UI.ActionButton("+1", () => {
                                progression.AdvanceExperienceTo(xpTable.GetBonus(nextLevel + 1), true);
                            }, UI.Width(63));
                        }
                        else { UI.Label("max", UI.Width(63)); }
                    }
                    else { UI.Space(66); }
                    UI.Space(10);
                    var nextML = progression.MythicExperience;
                    if (nextML <= mythicLevel || !isOnTeam)
                        UI.Label((mythicLevel < 10 ? "  my" : "  my").green() + $" {mythicLevel}", UI.Width(90));
                    else
                        UI.Label((level < 10 ? "  " : "") + $"{mythicLevel} > " + $"{nextML}".cyan(), UI.Width(90));
                    if (player.AllCharacters.Contains(ch)) {
                        if (progression.MythicExperience < 10) {
                            UI.ActionButton("+1", () => {
                                progression.AdvanceMythicExperience(progression.MythicExperience + 1, true);
                            }, UI.Width(63));
                        }
                        else { UI.Label("max", UI.Width(63)); }
                    }
                    else { UI.Space(66); }
                    UI.Space(30);
                    if (!isWide) ActionsGUI(ch);
                    UI.Wrap(!UI.IsWide, 283, 0);
                    var showClasses = ch == selectedCharacter && selectedToggle == ToggleChoice.Classes;
                    if (UI.DisclosureToggle($"{classData.Count} Classes", ref showClasses)) {
                        if (showClasses) {
                            selectedCharacter = ch; selectedToggle = ToggleChoice.Classes; Mod.Trace($"selected {ch.CharacterName}");
                        }
                        else { selectedToggle = ToggleChoice.None; }
                    }
                    var showStats = ch == selectedCharacter && selectedToggle == ToggleChoice.Stats;
                    if (UI.DisclosureToggle("Stats", ref showStats, 125)) {
                        if (showStats) { selectedCharacter = ch; selectedToggle = ToggleChoice.Stats; }
                        else { selectedToggle = ToggleChoice.None; }
                    }
                    UI.Wrap(UI.IsNarrow, 279);
                    //var showFacts = ch == selectedCharacter && selectedToggle == ToggleChoice.Facts;
                    //if (UI.DisclosureToggle("Facts", ref showFacts, 125)) {
                    //    if (showFacts) { selectedCharacter = ch; selectedToggle = ToggleChoice.Facts; }
                    //    else { selectedToggle = ToggleChoice.None; }
                    //}
                    var showFeatures = ch == selectedCharacter && selectedToggle == ToggleChoice.Features;
                    if (UI.DisclosureToggle("Features", ref showFeatures, 150)) {
                        if (showFeatures) { selectedCharacter = ch; selectedToggle = ToggleChoice.Features; }
                        else { selectedToggle = ToggleChoice.None; }
                    }
                    var showBuffs = ch == selectedCharacter && selectedToggle == ToggleChoice.Buffs;
                    if (UI.DisclosureToggle("Buffs", ref showBuffs, 125)) {
                        if (showBuffs) { selectedCharacter = ch; selectedToggle = ToggleChoice.Buffs; }
                        else { selectedToggle = ToggleChoice.None; }
                    }
                    UI.Wrap(UI.IsNarrow, 304);
                    var showAbilities = ch == selectedCharacter && selectedToggle == ToggleChoice.Abilities;
                    if (UI.DisclosureToggle("Abilities", ref showAbilities, 125)) {
                        if (showAbilities) { selectedCharacter = ch; selectedToggle = ToggleChoice.Abilities; }
                        else { selectedToggle = ToggleChoice.None; }
                    }
                    UI.Space(10);
                    if (spellbooks.Count() > 0) {
                        var showSpells = ch == selectedCharacter && selectedToggle == ToggleChoice.Spells;
                        if (UI.DisclosureToggle($"{spellCount} Spells", ref showSpells)) {
                            if (showSpells) { selectedCharacter = ch; selectedToggle = ToggleChoice.Spells; }
                            else { selectedToggle = ToggleChoice.None; }
                        }
                    }
                    else { UI.Space(180); }
                    if (isWide) ActionsGUI(ch);
                }
                //if (!UI.IsWide && (selectedToggle != ToggleChoice.Stats || ch != selectedCharacter)) {
                //    UI.Div(20, 20);
                //}
                if (selectedCharacter != spellbookEditCharacter) {
                    editSpellbooks = false;
                    spellbookEditCharacter = null;
                }
                if (ch == selectedCharacter && selectedToggle == ToggleChoice.Stats) {
                    UI.Div(100, 20, 755);
                    var alignment = ch.Descriptor.Alignment.ValueRaw;
                    using (UI.HorizontalScope()) {
                        UI.Space(100);
                        UI.Label("Alignment", UI.Width(425));
                        UI.Label($"{alignment.Name()}".color(alignment.Color()).bold(), UI.Width(1250f));
                    }
                    using (UI.HorizontalScope()) {
                        UI.Space(528);
                        UI.AlignmentGrid(alignment, (a) => ch.Descriptor.Alignment.Set(a));
                    }
                    UI.Div(100, 20, 755);
                    var alignmentMask = ch.Descriptor.Alignment.m_LockedAlignmentMask;
                    using (UI.HorizontalScope()) {
                        UI.Space(100);
                        UI.Label("Alignment Lock", UI.Width(425));
                        //UI.Label($"{alignmentMask.ToString()}".color(alignmentMask.Color()).bold(), UI.Width(325));
                        UI.Label($"Experimental - this sets a mask on your alignment shifts. {"Warning".bold().orange()}{": Using this may change your alignment.".orange()}".green());
                    }

                    using (UI.HorizontalScope()) {
                        UI.Space(528);
                        var maskIndex = Array.IndexOf(UI.AlignmentMasks, alignmentMask);
                        var titles = UI.AlignmentMasks.Select(
                            a => a.ToString().color(a.Color()).bold()).ToArray();
                        if (UI.SelectionGrid(ref maskIndex, titles, 3, UI.Width(800))) {
                            ch.Descriptor.Alignment.LockAlignment(UI.AlignmentMasks[maskIndex], new Alignment?());
                        }
                    }
                    UI.Div(100, 20, 755);
                    using (UI.HorizontalScope()) {
                        UI.Space(100);
                        UI.Label("Size", UI.Width(425));
                        var size = ch.Descriptor.State.Size;
                        UI.Label($"{size}".orange().bold(), UI.Width(175));
                    }
                    using (UI.HorizontalScope()) {
                        UI.Space(528);
                        UI.EnumGrid(
                            () => ch.Descriptor.State.Size,
                            (s) => ch.Descriptor.State.Size = s,
                            3, UI.Width(600));
                    }
                    using (UI.HorizontalScope()) {
                        UI.Space(528);
                        UI.ActionButton("Reset", () => { ch.Descriptor.State.Size = ch.Descriptor.OriginalSize; }, UI.Width(197));
                    }
                    UI.Div(100, 20, 755);
                    using (UI.HorizontalScope()) {
                        UI.Space(100);
                        UI.Label("Gender", UI.Width(400));
                        UI.Space(25);
                        var gender = ch.Descriptor.CustomGender ?? ch.Descriptor.Gender;
                        var isFemale = gender == Gender.Female;
                        using (UI.HorizontalScope(UI.Width(200))) {
                            if (UI.Toggle(isFemale ? "Female" : "Male", ref isFemale,
                                "♀".color(RGBA.magenta).bold(),
                                "♂".color(RGBA.aqua).bold(),
                                0, UI.largeStyle, GUI.skin.box, UI.Width(300), UI.Height(20))) {
                                ch.Descriptor.CustomGender = isFemale ? Gender.Female : Gender.Male;
                            }
                        }
                        UI.Label("Changing your gender may cause visual glitches".green());
                    }
                    UI.Space(10);
                    UI.Div(100, 20, 755);
                    foreach (var obj in HumanFriendly.StatTypes) {
                        var statType = obj;
                        var modifiableValue = ch.Stats.GetStat(statType);
                        if (modifiableValue == null) {
                            continue;
                        }

                        var key = $"{ch.CharacterName}-{statType}";
                        var storedValue = statEditorStorage.ContainsKey(key) ? statEditorStorage[key] : modifiableValue.BaseValue;
                        var statName = statType.ToString();
                        if (statName == "BaseAttackBonus" || statName == "SkillAthletics" || statName == "HitPoints") {
                            UI.Div(100, 20, 755);
                        }
                        using (UI.HorizontalScope()) {
                            UI.Space(100);
                            UI.Label(statName, UI.Width(400f));
                            UI.Space(25);
                            UI.ActionButton(" < ", () => {
                                modifiableValue.BaseValue -= 1;
                                storedValue = modifiableValue.BaseValue;
                            }, GUI.skin.box, UI.AutoWidth());
                            UI.Space(20);
                            UI.Label($"{modifiableValue.BaseValue}".orange().bold(), UI.Width(50f));
                            UI.ActionButton(" > ", () => {
                                modifiableValue.BaseValue += 1;
                                storedValue = modifiableValue.BaseValue;
                            }, GUI.skin.box, UI.AutoWidth());
                            UI.Space(25);
                            UI.ActionIntTextField(ref storedValue, (v) => {
                                modifiableValue.BaseValue = v;
                            }, UI.Width(75));
                            statEditorStorage[key] = storedValue;
                        }
                    }
                }
                //if (ch == selectedCharacter && selectedToggle == ToggleChoice.Facts) {
                //    FactsEditor.OnGUI(ch, ch.Facts.m_Facts);
                //}
                if (ch == selectedCharacter && selectedToggle == ToggleChoice.Features) {
                    FactsEditor.OnGUI(ch, ch.Progression.Features.Enumerable.ToList());
                }
                if (ch == selectedCharacter && selectedToggle == ToggleChoice.Buffs) {
                    FactsEditor.OnGUI(ch, ch.Descriptor.Buffs.Enumerable.ToList());
                }
                if (ch == selectedCharacter && selectedToggle == ToggleChoice.Abilities) {
                    FactsEditor.OnGUI(ch, ch.Descriptor.Abilities.Enumerable.ToList());
                }
                if (ch == selectedCharacter && selectedToggle == ToggleChoice.Spells) {
                    UI.Space(20);
                    var names = spellbooks.Select((sb) => sb.Blueprint.GetDisplayName()).ToArray();
                    var titles = names.Select((name, i) => $"{name} ({spellbooks.ElementAt(i).CasterLevel})").ToArray();
                    if (spellbooks.Any()) {
                        using (UI.HorizontalScope()) {
                            UI.SelectionGrid(ref selectedSpellbook, titles, Math.Min(titles.Length, 7), UI.AutoWidth());
                            if (selectedSpellbook >= names.Length) selectedSpellbook = 0;
                            UI.DisclosureToggle("Edit".orange().bold(), ref editSpellbooks);
                        }
                        var spellbook = spellbooks.ElementAt(selectedSpellbook);
                        if (editSpellbooks) {
                            spellbookEditCharacter = ch;
                            var blueprints = BlueprintExensions.GetBlueprints<BlueprintSpellbook>().OrderBy((bp) => bp.GetDisplayName());
                            BlueprintListUI.OnGUI(ch, blueprints, 100);
                        }
                        else {
                            var maxLevel = spellbook.Blueprint.MaxSpellLevel;
                            var casterLevel = spellbook.CasterLevel;
                            using (UI.HorizontalScope()) {
                                UI.EnumerablePicker<int>(
                                    "Spells known",
                                    ref selectedSpellbookLevel,
                                    Enumerable.Range(0, spellbook.Blueprint.MaxSpellLevel + 1),
                                    0,
                                    (lvl) => {
                                        var levelText = spellbook.Blueprint.SpellsPerDay.GetCount(casterLevel, lvl) != null ? $"L{lvl}".bold() : $"L{lvl}".grey();
                                        var knownCount = spellbook.GetKnownSpells(lvl).Count;
                                        var countText = knownCount > 0 ? $" ({knownCount})".white() : "";
                                        return levelText + countText;
                                    },
                                    UI.AutoWidth()
                                );
                                UI.Space(20);
                                if (casterLevel > 0) {
                                    UI.ActionButton("-1 CL", () => CasterHelpers.LowerCasterLevel(spellbook), UI.AutoWidth());
                                }
                                if (casterLevel < 40) {
                                    UI.ActionButton("+1 CL", () => CasterHelpers.AddCasterLevel(spellbook), UI.AutoWidth());
                                }

                                UI.Space(20);
                                if (ch.Spellbooks.Where(x => x.IsStandaloneMythic && !spellbook.IsStandaloneMythic && x.Blueprint.CharacterClass != null).Any(y => y.Blueprint.CharacterClass == ch.Progression.GetMythicToMerge()?.CharacterClass)) {
                                    using (UI.VerticalScope()) {
                                        using (UI.HorizontalScope()) {
                                            UI.ActionButton("Merge Mythic Levels and Selected Spellbook", () => CasterHelpers.ForceSpellbookMerge(spellbook), UI.AutoWidth());
                                            UI.Label("Warning: This is irreversible. Please save before continuing!".Orange());
                                        }

                                        UI.Label("Merging your mythic spellbook will cause you to transfer all mythic spells to your normal spellbook and gain caster levels equal to your mythic level. You will then be able to re-select spells on next level up or mythic level up.", UI.Width(850));
                                    }
                                }
                            }
                            SelectedSpellbook[ch.HashKey()] = spellbook;
                            FactsEditor.OnGUI(ch, spellbook, selectedSpellbookLevel);
                        }
                    }
#if false
                    else {
                        spellbookEditCharacter = ch;
                        editSpellbooks = true;
                        var blueprints = BlueprintExensions.GetBlueprints<BlueprintSpellbook>().OrderBy((bp) => bp.GetDisplayName());
                        BlueprintListUI.OnGUI(ch, blueprints, 100);
                    }
#endif
                }
                if (selectedCharacter != GetSelectedCharacter()) {
                    selectedCharacterIndex = characterList.IndexOf(selectedCharacter);
                }
                chIndex += 1;
            }
            UI.Space(25);
            if (recruitableCount > 0) {
                UI.Label($"{recruitableCount} character(s) can be ".orange().bold() + " Recruited".cyan() + ". This allows you to add non party NPCs to your party as if they were mercenaries".green());
            }
            if (respecableCount > 0) {
                UI.Label($"{respecableCount} character(s)  can be ".orange().bold() + "Respecced".cyan() + ". Pressing Respec will close the mod window and take you to character level up".green());
                UI.Label("WARNING".yellow().bold() + " The Respec UI is ".orange() + "Non Interruptable".yellow().bold() + " please save before using".orange());
            }
            if (recruitableCount > 0 || respecableCount > 0) {
                UI.Label("WARNING".yellow().bold() + " these features are ".orange() + "EXPERIMENTAL".yellow().bold() + " and uses unreleased and likely buggy code.".orange());
                UI.Label("BACK UP".yellow().bold() + " before playing with this feature.You will lose your mythic ranks but you can restore them in this Party Editor.".orange());
            }
            UI.Space(25);
            if (charToAdd != null) { UnitEntityDataUtils.AddCompanion(charToAdd); }
            if (charToRecruit != null) { UnitEntityDataUtils.AddCompanion(charToRecruit); }
            if (charToRemove != null) { UnitEntityDataUtils.RemoveCompanion(charToRemove); }
        }
    }
}