using Modding;
using System;
using System.Collections;
using HutongGames.PlayMaker.Actions;
using HKMirror;
using Satchel.BetterMenus;

namespace LifebloodHealing
{
    #region Menu
    public static class ModMenu
    {
        private static Menu? MenuRef;
        public static MenuScreen CreateModMenu(MenuScreen modlistmenu)
        {
            MenuRef ??= new Menu("Lifeblood Healing Options", new Element[]
            {
                Blueprints.HorizontalBoolOption
                (
                    "Mod Enabled",
                    "Does this mod do anything?",
                    (b) =>
                    {
                        LifebloodHealingMod.LS.active = b;
                    },
                    () => LifebloodHealingMod.LS.active
                ),

                Blueprints.HorizontalBoolOption
                (
                    "Require Joni's Blessing",
                    "Healing all Lifeblood requires Joni's Blessing",
                    (b) =>
                    {
                        LifebloodHealingMod.LS.requireJonis = b;
                    },
                    () => LifebloodHealingMod.LS.requireJonis
                ),

                Blueprints.HorizontalBoolOption
                (
                    "Lifeblood Charms",
                    "Can you heal Lifeblood from Heart/Core?",
                    (b) =>
                    {
                        LifebloodHealingMod.LS.lifebloodCharms = b;
                    },
                    () => LifebloodHealingMod.LS.lifebloodCharms
                ),

                Blueprints.HorizontalBoolOption
                (
                    "Lifeblood Cocoons",
                    "Can you heal Lifeblood from Cocoons?",
                    (b) =>
                    {
                        LifebloodHealingMod.LS.lifebloodCocoons = b;
                    },
                    () => LifebloodHealingMod.LS.lifebloodCocoons
                ),

                Blueprints.HorizontalBoolOption
                (
                    "Focusing Continues",
                    "Can you heal multiple Lifeblood during a single Focus?",
                    (b) =>
                    {
                        LifebloodHealingMod.LS.ignoreMaxHealth = b;
                    },
                    () => LifebloodHealingMod.LS.ignoreMaxHealth
                )
            });
            return MenuRef.GetMenuScreen(modlistmenu);
        }
    }
    #endregion
    public class LifebloodHealingMod : Mod, ICustomMenuMod, ILocalSettings<LocalSettings>
    {
        #region Boilerplate
        private static LifebloodHealingMod? _instance;
        internal static LifebloodHealingMod Instance
        {
            get
            {
                if (_instance == null)
                {
                    throw new InvalidOperationException($"An instance of {nameof(LifebloodHealingMod)} was never constructed");
                }
                return _instance;
            }
        }

        public MenuScreen GetMenuScreen(MenuScreen modListMenu, ModToggleDelegates? toggleDelegates) => ModMenu.CreateModMenu(modListMenu);
        public bool ToggleButtonInsideMenu => false;
        public static LocalSettings LS { get; private set; } = new();
        public void OnLoadLocal(LocalSettings s) => LS = s;
        public LocalSettings OnSaveLocal() => LS;
        public override string GetVersion() => GetType().Assembly.GetName().Version.ToString();

        public LifebloodHealingMod() : base("LifebloodHealing")
        {
            _instance = this;
        }
        #endregion

        #region CustomVars
        public int joniHealth = 0;
        #endregion

        #region Init
        public override void Initialize()
        {
            Log("Initializing");

            On.HutongGames.PlayMaker.Actions.IntCompare.OnEnter += FullHealthCheck;
            On.HutongGames.PlayMaker.Actions.CallMethodProper.OnEnter += OnFocusHeal;
            ModHooks.CharmUpdateHook += CalcJoniHealth;
            On.ScuttlerControl.Heal += LifebloodCocoons;

            Log("Initialized");
        }
        #endregion

        #region Changes
        private void OnFocusHeal(On.HutongGames.PlayMaker.Actions.CallMethodProper.orig_OnEnter orig, CallMethodProper self)
        {
            if (self.Fsm.GameObject.name == "Knight" && self.Fsm.Name == "Spell Control" && self.methodName.Value == "AddHealth")
            {
                if (LS.active && (!LS.requireJonis || PlayerDataAccess.equippedCharm_27) && PlayerDataAccess.healthBlue < joniHealth && PlayerDataAccess.health == PlayerDataAccess.maxHealth)
                {
                    EventRegister.SendEvent("ADD BLUE HEALTH");
                }
            }

            orig(self);
        }
        private void CalcJoniHealth(PlayerData pd, HeroController hc)
        {
            joniHealth = pd.joniHealthBlue;
            if (LS.active && LS.lifebloodCharms && (!LS.requireJonis || PlayerDataAccess.equippedCharm_27))
            {
                joniHealth += (PlayerDataAccess.equippedCharm_8 ? 2 : 0) + (PlayerDataAccess.equippedCharm_9 ? 4 : 0);
            }

        }
        private IEnumerator LifebloodCocoons(On.ScuttlerControl.orig_Heal orig, ScuttlerControl self)
        {
            joniHealth += (LS.active && LS.lifebloodCocoons && (!LS.requireJonis || PlayerDataAccess.equippedCharm_27)) ? 1 : 0;
            return orig(self);
        }
        private void FullHealthCheck(On.HutongGames.PlayMaker.Actions.IntCompare.orig_OnEnter orig, IntCompare self)
        {
            if (self.Fsm.GameObject.name == "Knight" && self.Fsm.Name == "Spell Control" && self.State.Name.StartsWith("Full HP?") && self.integer1.Name == "HP")
            {
                self.integer1.Value = (LS.active && LS.ignoreMaxHealth && PlayerDataAccess.healthBlue < joniHealth) ? 0 : self.integer1.Value;
            }

            orig(self);
        }
        #endregion
    }

    #region LocalSettings
    public class LocalSettings
    {
        public bool active = true;
        public bool requireJonis = true;
        public bool lifebloodCharms = false;
        public bool lifebloodCocoons = false;
        public bool ignoreMaxHealth = false;
    }
    #endregion
}
