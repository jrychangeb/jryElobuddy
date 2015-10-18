using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using SharpDX;
using Color = System.Drawing.Color;
using SpellData = RivenBuddy.DamageIndicator.SpellData;

namespace RivenBuddy
{
    internal class Program
    {
        public static Menu Menu, ComboMenu, HarassMenu, MinionClear, Jungle, DrawMenu;
        public static bool checkAA = false;
        public static Text text = new Text("", new Font(FontFamily.GenericSansSerif, 9, FontStyle.Bold));
        public static DamageIndicator.DamageIndicator Indicator;
        public static Spell.Skillshot R2;

        public static bool IsRActive
        {
            get { return ComboMenu["useR"].Cast<KeyBind>().CurrentValue; }
        }

        public static bool BurstActive
        {
            get { return ComboMenu["burst"].Cast<KeyBind>().CurrentValue; }
        }

        private static void Main(string[] args)
        {
            Loading.OnLoadingComplete += Loading_OnLoadingComplete;
        }

        private static void Loading_OnLoadingComplete(EventArgs args)
        {
            if (Player.Instance.ChampionName != Champion.Riven.ToString()) return;

            Menu = MainMenu.AddMenu("瑞文", "rivenbuddy");
            Menu.AddGroupLabel("Riven Buddy");
            Menu.AddSeparator();
            Menu.AddLabel("By Fluxy 汉化：鬼灵 MapieA");
            Menu.AddLabel("nixi waz here");

            ComboMenu = Menu.AddSubMenu("连招菜单", "combosettingsRiven");
            ComboMenu.AddGroupLabel("连招设置");
            ComboMenu.Add("combo.useQ", new CheckBox("使用 Q"));
            ComboMenu.Add("combo.useQGapClose", new CheckBox("Use Q to Gapclose", false));
            ComboMenu.Add("combo.useW", new CheckBox("使用 W"));
            ComboMenu.Add("combo.useE", new CheckBox("使用 E"));
            ComboMenu.Add("combo.useR", new CheckBox("使用 R"));
            ComboMenu.Add("combo.useR2", new CheckBox("使用 R2"));
            ComboMenu.Add("combo.hydra", new CheckBox("使用提亚玛特/九头蛇"));
            ComboMenu.Add("useR", new KeyBind("Force R", false, KeyBind.BindTypes.PressToggle, 'T'));
            ComboMenu.AddSeparator();
            ComboMenu.AddLabel("R1 连招");
            ComboMenu.Add("combo.eR1", new CheckBox("E -> R1"));
            ComboMenu.Add("combo.R1", new CheckBox("R1"));
            ComboMenu.AddSeparator();
            ComboMenu.AddLabel("R2 连招");
            ComboMenu.Add("combo.eR2", new CheckBox("E -> R2"));
            ComboMenu.Add("combo.qR2", new CheckBox("R2 -> Q"));
            ComboMenu.Add("combo.R2", new CheckBox("R2"));
            ComboMenu.AddSeparator();
            ComboMenu.AddGroupLabel("突进连招");
            ComboMenu.Add("burst.flash", new CheckBox("使用闪现突进"));
            ComboMenu.Add("burst", new KeyBind("Burst", false, KeyBind.BindTypes.HoldActive, 'Y'));
            ComboMenu.AddSeparator();
            ComboMenu.AddGroupLabel("杂项");
            ComboMenu.Add("combo.keepQAlive", new CheckBox("Keep Q Alive"));
            ComboMenu.Add("combo.useRBeforeExpire", new CheckBox("Use R Before Expire"));
            ComboMenu.Add("combo.alwaysCancelQ", new CheckBox("Always Cancel Q", false));

            HarassMenu = Menu.AddSubMenu("骚扰菜单", "harasssettingsRiven");
            HarassMenu.AddGroupLabel("骚扰设置");
            HarassMenu.Add("harass.hydra", new CheckBox("使用提亚玛特/九头蛇"));
            HarassMenu.Add("harass.useQ", new CheckBox("使用 Q"));
            HarassMenu.Add("harass.useW", new CheckBox("使用 W"));
            HarassMenu.Add("harass.useE", new CheckBox("使用 E"));

            MinionClear = Menu.AddSubMenu("刷兵菜单", "farmettingsRiven");
            MinionClear.AddGroupLabel("尾刀/控线");
            MinionClear.Add("lasthit.useQ", new CheckBox("使用 Q"));
            MinionClear.Add("lasthit.useW", new CheckBox("使用 W"));
            MinionClear.AddSeparator();
            MinionClear.AddGroupLabel("清线设置");
            MinionClear.Add("waveclear.hydra", new CheckBox("使用提亚玛特/九头蛇"));
            MinionClear.Add("waveclear.useQ", new CheckBox("使用 Q"));
            MinionClear.Add("waveclear.useW", new CheckBox("使用 W"));

            Jungle = Menu.AddSubMenu("清野菜单", "jungleettingsRiven");
            Jungle.AddGroupLabel("清野设置");
            Jungle.Add("jungle.hydra", new CheckBox("使用提亚玛特/九头蛇"));
            Jungle.Add("jungle.useQ", new CheckBox("使用 Q"));
            Jungle.Add("jungle.useW", new CheckBox("使用 W"));
            Jungle.Add("jungle.useE", new CheckBox("使用 E"));

            DrawMenu = Menu.AddSubMenu("线圈菜单", "drawsettingsRiven");
            DrawMenu.AddGroupLabel("线圈设置");
            DrawMenu.Add("draw.Q", new CheckBox("Q 范围", false));
            DrawMenu.Add("draw.W", new CheckBox("W 范围", false));
            DrawMenu.Add("draw.E", new CheckBox("E 范围", false));
            DrawMenu.Add("draw.R", new CheckBox("R 范围", false));
            DrawMenu.Add("draw.Damage", new CheckBox("显示技能伤害"));
            DrawMenu.Add("draw.Combo", new CheckBox("显示连招伤害", false));
            DrawMenu.Add("draw.rState", new CheckBox("显示 R 状态"));

            Indicator = new DamageIndicator.DamageIndicator();
            Indicator.Add("Combo", new SpellData(0, DamageType.True, Color.Aqua));

            R2 = new Spell.Skillshot(SpellSlot.R, 900, SkillShotType.Cone, 250, 1600, 125);
            TargetSelector2.Init();
            SpellEvents.Init();
            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnUpdate += delegate { SpellManager.UpdateSpells(); };
            Player.OnIssueOrder += Player_OnIssueOrder;

            Chat.Print("RivenBuddy : Fully Loaded. by fluxy");
        }

        public static GameObject OrderTarget;
        public static Vector3 OrderPosition;
        public static GameObjectOrder Order;

        private static void Player_OnIssueOrder(Obj_AI_Base sender, PlayerIssueOrderEventArgs args)
        {
            if (!sender.IsMe) return;
            Order = args.Order;
            OrderPosition = args.TargetPosition;
            OrderTarget = args.Target;
        }

        public static void IssueLastOrder()
        {
            switch (Order)
            {
                    case GameObjectOrder.AttackUnit:
                    if (OrderTarget != null) Player.IssueOrder(Order, OrderTarget);
                    break;

                case GameObjectOrder.MoveTo:
                    Player.IssueOrder(Order, OrderPosition);
                    break;
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            var pos = Drawing.WorldToScreen(Player.Instance.Position);
            if (DrawMenu["draw.rState"].Cast<CheckBox>().CurrentValue)
                text.Draw("Forced R: " + IsRActive, Color.AliceBlue, (int) pos.X - 45,
                    (int) pos.Y + 40);
            /*
            foreach (var position in WallJump.Spots.Where(a => a.Start.Distance(Player.Instance) < 400))
            {
                Circle.Draw(SharpDX.Color.OrangeRed,
                    100, position.Start);
                Circle.Draw(SharpDX.Color.DarkCyan,
                     100, position.End);
            }
            */

            if (DrawMenu["draw.Combo"].Cast<CheckBox>().CurrentValue)
            {
                var s = Queuer.Queue.Aggregate("", (current, VARIABLE) => current + (" " + VARIABLE));
                Drawing.DrawText(100, 100, Color.Wheat, s);
            }
            if (DrawMenu["draw.Q"].Cast<CheckBox>().CurrentValue)
            {
                Circle.Draw(SpellManager.Spells[SpellSlot.Q].IsReady() ? SharpDX.Color.Cyan : SharpDX.Color.OrangeRed,
                    SpellManager.Spells[SpellSlot.Q].Range, Player.Instance.Position);
            }
            if (DrawMenu["draw.W"].Cast<CheckBox>().CurrentValue)
            {
                Circle.Draw(SpellManager.Spells[SpellSlot.W].IsReady() ? SharpDX.Color.Cyan : SharpDX.Color.OrangeRed,
                    SpellManager.Spells[SpellSlot.W].Range, Player.Instance.Position);
            }
            if (DrawMenu["draw.E"].Cast<CheckBox>().CurrentValue)
            {
                Circle.Draw(SpellManager.Spells[SpellSlot.E].IsReady() ? SharpDX.Color.Cyan : SharpDX.Color.OrangeRed,
                    SpellManager.Spells[SpellSlot.E].Range, Player.Instance.Position);
            }
            if (DrawMenu["draw.R"].Cast<CheckBox>().CurrentValue)
            {
                Circle.Draw(SpellManager.Spells[SpellSlot.R].IsReady() ? SharpDX.Color.Cyan : SharpDX.Color.OrangeRed,
                    SpellManager.Spells[SpellSlot.R].Range, Player.Instance.Position);
            }
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            Orbwalker.ForcedTarget = null;
            Queuer.tiamat =
                ObjectManager.Player.InventoryItems.FirstOrDefault(
                    a => a.Id == ItemId.Tiamat_Melee_Only || a.Id == ItemId.Ravenous_Hydra_Melee_Only);

            Indicator.Update("Combo", new SpellData((int) DamageHandler.ComboDamage(), DamageType.Physical, Color.Aqua));
            if (BurstActive)
            {
                States.Burst();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                States.Combo();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
            {
                States.Harass();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear))
            {
                States.Jungle();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
            {
                var target = EntityManager.MinionsAndMonsters.GetJungleMonsters(Player.Instance.Position,
                    SpellManager.Spells[SpellSlot.Q].Range + 300).OrderByDescending(a => a.MaxHealth).FirstOrDefault();
                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear) && target == null)
                {
                    States.WaveClear();
                }
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LastHit))
            {
                States.LastHit();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Flee))
            {
                States.Flee();
            }
            if (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.None && !BurstActive)
            {
                Queuer.Queue = new List<string>();
            }
        }
    }
}