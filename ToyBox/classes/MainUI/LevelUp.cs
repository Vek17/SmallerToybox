using ModKit;

namespace ToyBox {
    public class LevelUp {
        public static Settings settings => Main.settings;
        public static void ResetGUI() { }
        public static void OnGUI() {
            UI.HStack("Character Creation", 1,
                () => UI.Slider("Build Points (Main)", ref settings.characterCreationAbilityPointsPlayer, 1, 200, 25, "", UI.AutoWidth()),
                () => UI.Slider("Build Points (Mercenary)", ref settings.characterCreationAbilityPointsMerc, 1, 200, 20, "", UI.AutoWidth()),
                () => UI.Slider("Ability Max", ref settings.characterCreationAbilityPointsMax, 0, 50, 18, "", UI.AutoWidth()),
                () => UI.Slider("Ability Min", ref settings.characterCreationAbilityPointsMin, 0, 50, 7, "", UI.AutoWidth()),
                //() => {
                //    UI.Toggle("All Appearance Options", ref settings.toggleAllRaceCustomizations);
                //    UI.Space(25);
                //    UI.Label("Allows you to choose all appearance options from any race".green());
                //},
                () => { }
                );
            UI.Div(0, 25);
            UI.HStack("Mythic Paths", 1,
                () => UI.Label("Warning! Using these might break your game somehow. Recommend for experimental tinkering like trying out different builds, and not for actually playing the game.".green()),
                () => UI.ActionButton("Unlock Aeon", Actions.UnlockAeon, UI.Width(300)),
                () => UI.ActionButton("Unlock Azata", Actions.UnlockAzata, UI.Width(300)),
                () => UI.ActionButton("Unlock Trickster", Actions.UnlockTrickster, UI.Width(300)),
                () => { UI.ActionButton("Unlock Swarm", Actions.UnlockSwarm, UI.Width(300)); UI.Space(25); UI.Label("Only available at Mythic level 8 or higher".green()); },
                () => { UI.ActionButton("Unlock Gold Dragon", Actions.UnlockGoldDragon, UI.Width(300)); UI.Space(25); UI.Label("Only available at Mythic level 8 or higher".green()); },
                () => {
                    UI.ActionButton("All Mythic Paths".orange(), Actions.UnlockAllBasicMythicPaths, UI.Width(300));
                    UI.Space(25);
                    UI.Label("Unlock mythic paths besides Legend and Devil which block progression".green());
                },
                () => UI.Label("", UI.Height(10)),
                () => { UI.ActionButton("Unlock Devil", Actions.UnlockDevil, UI.Width(300)); UI.Space(25); UI.Label("Prevents you from advancing in Aeon or Azata".green()); },
                () => { UI.ActionButton("Unlock Legend", Actions.UnlockLegend, UI.Width(300)); UI.Space(25); UI.Label("Prevents you from advancing all other Mythic Path".green()); },
                () => { }
                );
            UI.Div(0, 25);
            UI.HStack("Create & Level Up", 1,
                () => UI.Toggle("Enable  'Next' when no feat selections are available", ref settings.toggleNextWhenNoAvailableFeatSelections),
                () => UI.Toggle("Make All Feature Selections Optional", ref settings.toggleOptionalFeatSelection),
                () => {
                    UI.Toggle("Ignore Attribute Cap", ref settings.toggleIgnoreAttributeCap);
                    UI.Space(25);
                    UI.Toggle("Ignore Remaining Attribute Points", ref settings.toggleIgnoreAttributePointsRemaining);
                },
                () => {
                    UI.Toggle("Ignore Skill Cap", ref settings.toggleIgnoreSkillCap);
                    UI.Space(73);
                    UI.Toggle("Ignore Remaining Skill Points", ref settings.toggleIgnoreSkillPointsRemaining);
                },
                () => UI.Toggle("Always Able To Level Up", ref settings.toggleNoLevelUpRestrictions),
                () => UI.Toggle("Add Full Hit Die Value", ref settings.toggleFullHitdiceEachLevel),
                () => {
                    UI.Toggle("Ignore Class And Feat Restrictions", ref settings.toggleIgnoreClassAndFeatRestrictions);
                    UI.Space(25);
                    UI.Label("Experimental".cyan() + ": in addition to regular leveling, this allows you to choose any mythic class each time you level up starting from level 1. This may have interesting and unexpected effects. Backup early and often...".green());
                },
                () => UI.Toggle("Allow Companions to Take Mythic Classes", ref settings.toggleAllowCompanionsToBecomeMythic),
                () => UI.Toggle("Ignore Prerequisites When Choosing A Feat", ref settings.toggleFeaturesIgnorePrerequisites),
                () => UI.Toggle("Ignore Caster Type And Spell Level Restrictions", ref settings.toggleIgnoreCasterTypeSpellLevel),
                () => UI.Toggle("Ignore Forbidden Archetypes", ref settings.toggleIgnoreForbiddenArchetype),
                () => UI.Toggle("Ignore Required Stat Values", ref settings.toggleIgnorePrerequisiteStatValue),
                () => UI.Toggle("Ignore Alignment When Choosing A Class", ref settings.toggleIgnoreAlignmentWhenChoosingClass),
                () => UI.Toggle("Ignore Prerequisite Features (like Race) when choosing Class", ref settings.toggleIgnoreFeaturePrerequisitesWhenChoosingClass),
#if false // This is incredibly optimistic and requires resolving a bunch of conflicts with the existing gestalt and scroll copy logic
                () => UI.Toggle("Ignore Spellbook Restrictions When Choosing Spells", ref settings.toggleUniversalSpellbookd),
#endif

                () => UI.Toggle("Skip Spell Selection", ref settings.toggleSkipSpellSelection),
#if DEBUG
                    () => UI.Toggle("Lock Character Level", ref settings.toggleLockCharacterLevel),
                //                    () => UI.Toggle("Ignore Alignment Restrictions", ref settings.toggleIgnoreAlignmentRestriction),
#endif
#if false
                // Do we need these or is it covered by toggleFeaturesIgnorePrerequisites
                () => { UI.Toggle("Ignore Feat Prerequisites When Choosing A Class", ref settings.toggleIgnoreFeaturePrerequisitesWhenChoosingClass); },
                () => { UI.Toggle("Ignore Feat Prerequisits (List) When Choosing A Class", ref settings.toggle); },
#endif

                () => { }
                );
#if true
#if false
                () => UI.Toggle("Use Recalculate Caster Levels", ref settings.toggleRecalculateCasterLevelOnLevelingUp),
                () => UI.Toggle("Restrict Caster Level To Current", ref settings.toggleRestrictCasterLevelToCharacterLevel),
                //() => { UI.Toggle("Restrict CL to Current (temp) ", ref settings.toggleRestrictCasterLevelToCharacterLevelTemporary),
                () => UI.Toggle("Restrict Class Level for Prerequisites to Caster Level", ref settings.toggleRestrictClassLevelForPrerequisitesToCharacterLevel),
                () => UI.Toggle("Fix Favored Class HP", ref settings.toggleFixFavoredClassHP),
                () => UI.Toggle("Always Receive Favored Class HP", ref settings.toggleAlwaysReceiveFavoredClassHP),
                () => UI.Toggle("Always Receive Favored Class HP Except Prestige", ref settings.toggleAlwaysReceiveFavoredClassHPExceptPrestige),
#endif
#endif
        }
    }
}
