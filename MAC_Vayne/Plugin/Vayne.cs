using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using MAC_Vayne.Util;
using SharpDX;
using Color = System.Drawing.Color;

namespace MAC_Vayne.Plugin
{
    static class Vayne
    {

        #region Global Variables

        /*
         Config
         */

        public static string G_version = "1.2.1";
        public static string G_charname = _Player.ChampionName;

        /*
         Spells
         */
        public static Spell.Ranged Q;
        public static Spell.Targeted E;
        public static Spell.Active R;

        /*
         Menus
         */

        public static Menu Menu,
            ComboMenu,
            LaneClearMenu,
            CondemnMenu,
            KSMenu,
            DrawMenu;

        /*
         Misc
         */

        public static AIHeroClient _target;

        #endregion

        #region Initialization

        public static AIHeroClient _Player
        {
            get { return ObjectManager.Player; }
        }

        public static void Init()
        {
            Bootstrap.Init(null);
            
            InitVariables();

            Orbwalker.OnPostAttack += OnAfterAttack;
            Orbwalker.OnPreAttack += OnBeforeAttack;
            Gapcloser.OnGapcloser += OnGapCloser;
            Interrupter.OnInterruptableSpell += OnPossibleToInterrupt;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpell;

            Game.OnUpdate += OnGameUpdate;
            Drawing.OnDraw += OnDraw;
        }

        public static void InitVariables()
        {
            Q = new Spell.Skillshot(SpellSlot.Q, 325, SkillShotType.Linear);
            E = new Spell.Targeted(SpellSlot.E, 550);
            R = new Spell.Active(SpellSlot.R);
            InitMenu();
        }

        public static void InitMenu()
        {
            Menu = MainMenu.AddMenu("MAC - " + G_charname, "vania");

            Menu.AddGroupLabel("MAC - " + G_charname);
            Menu.AddLabel("Version: " + G_version);
            Menu.AddSeparator();
            Menu.AddLabel("By Mr Articuno 汉化—MapieA");

            /*Brain.Common.Selector.Init(Menu);*/

            DrawMenu = Menu.AddSubMenu("线圈 - " + G_charname, "vaniaDraw");
            DrawMenu.AddGroupLabel("线圈");
            DrawMenu.Add("禁用线圈", new CheckBox("禁用所有线圈", false));
            DrawMenu.Add("drawNameLine", new CheckBox("Show names on line", true));
            DrawMenu.Add("平A范围", new CheckBox("画出自己平A范围", true));
            DrawMenu.Add("Q范围", new CheckBox("Q-线圈", true));
            DrawMenu.Add("E范围", new CheckBox("E-线圈", true));
            DrawMenu.Add("drawCondemnPos", new CheckBox("Draw Condemn Position", true));

            ComboMenu = Menu.AddSubMenu("连招 - " + G_charname, "vaniaCombo");
            ComboMenu.AddGroupLabel("连招");
            ComboMenu.Add("Q-连招", new CheckBox("使用Q连招", true));
            ComboMenu.Add("E-连招", new CheckBox("使用E连招", true));
            ComboMenu.Add("R-连招", new CheckBox("使用R连招", true));
            ComboMenu.AddGroupLabel("Q 设置");
            ComboMenu.AddLabel("Q 方向: 选中 - 目标, 跟随鼠标");
            ComboMenu.Add("qsQDirection", new CheckBox("Q 方向", false));
            ComboMenu.AddLabel("Q 使用: 选择之前 - 自动攻击, Unchecked After Auto Attack");
            ComboMenu.Add("qsQUsage", new CheckBox("Q 使用", false));
            ComboMenu.Add("qsQOutAA", new CheckBox("AA后使用Q", true));
            ComboMenu.AddGroupLabel("R 设置");
            ComboMenu.Add("rsMinEnemiesForR", new Slider("范围内多少敌人使用R: ", 2, 1, 5));
            ComboMenu.AddGroupLabel("杂项");
            /*ComboMenu.Add("advTargetSelector", new CheckBox("Use Advanced Target Selector", false));*/
            ComboMenu.Add("forceSilverBolt", new CheckBox("Force Attack 2 Stacked Target", false));
            ComboMenu.Add("checkKillabeEnemyPassive", new CheckBox("Double Check if enemy is killabe", true));

            CondemnMenu = Menu.AddSubMenu("Condemn - " + G_charname, "vaniaCondemn");
            CondemnMenu.Add("interruptDangerousSpells", new CheckBox("Interrupt Dangerous Spells", true));
            CondemnMenu.Add("antiGapCloser", new CheckBox("Anti Gap Closer", true));
            CondemnMenu.Add("fastCondemn",
                new KeyBind("Fast Condemn HotKey", false, KeyBind.BindTypes.PressToggle, 'W'));
            CondemnMenu.AddGroupLabel("Auto Condemn");
            foreach (var enemy in ObjectManager.Get<AIHeroClient>().Where(a => a.IsEnemy))
            {
                CondemnMenu.Add("dnCondemn" + enemy.ChampionName.ToLower(), new CheckBox("Don't Condemn " + enemy.ChampionName, false));
            }
            CondemnMenu.AddGroupLabel("Priority Condemn");
            foreach (var enemy in ObjectManager.Get<AIHeroClient>().Where(a => a.IsEnemy))
            {
                CondemnMenu.Add("priorityCondemn" + enemy.ChampionName.ToLower(), new Slider(enemy.ChampionName + " Priority", 1, 1, 5));
            }
            CondemnMenu.Add("condenmErrorMargin", new Slider("Subtract Condemn Push by: ", 20, 0, 100));

            KSMenu = Menu.AddSubMenu("KS - " + G_charname, "vaniaKillSteal");
            KSMenu.AddGroupLabel("Kill Steal");
            KSMenu.Add("ksQ", new CheckBox("Use Q if killable", false));
            KSMenu.Add("ksE", new CheckBox("Use E if killable", false));
        }

        #endregion

        public static void OnDraw(EventArgs args)
        {
            if (Misc.isChecked(DrawMenu, "drawDisable"))
                return;

            if (Misc.isChecked(DrawMenu, "drawAARange"))
            {
                new Circle() { Color = Color.Cyan, Radius = _Player.GetAutoAttackRange(), BorderWidth = 2f }.Draw(_Player.Position);
                if (Misc.isChecked(DrawMenu, "drawNameLine"))
                    Drawing.DrawText(Drawing.WorldToScreen(_Player.Position) - new Vector2(_Player.GetAutoAttackRange() - 250, 0), Color.Cyan, "Auto Attack", 15);
            }

            if (Misc.isChecked(DrawMenu, "drawQ") && Q.IsReady())
            {
                new Circle() { Color = Color.White, Radius = Q.Range, BorderWidth = 2f }.Draw(_Player.Position);
                if (Misc.isChecked(DrawMenu, "drawNameLine"))
                    Drawing.DrawText(Drawing.WorldToScreen(_Player.Position) - new Vector2(Q.Range - 100, 0), Color.White, "Q Range", 15);
            }

            if (Misc.isChecked(DrawMenu, "drawE") && E.IsReady())
            {

                new Circle() { Color = Color.White, Radius = E.Range, BorderWidth = 2f }.Draw(_Player.Position);
                if (Misc.isChecked(DrawMenu, "drawNameLine"))
                    Drawing.DrawText(Drawing.WorldToScreen(_Player.Position) - new Vector2(E.Range - 290, -50), Color.White, "E Range", 15);
            }

            if (Misc.isChecked(DrawMenu, "drawCondemnPos") && E.IsReady())
            {
                foreach (var enemy in ObjectManager.Get<AIHeroClient>().Where(a => a.IsEnemy).Where(a => !a.IsDead).Where(a => _Player.Distance(a) <= E.Range))
                {
                    if (Misc.isChecked(CondemnMenu, "dnCondemn" + enemy.ChampionName.ToLower()))
                        return;

                    var condemnPos = _Player.Position.Extend(enemy.Position, _Player.Distance(enemy) + 470 - Misc.getSliderValue(CondemnMenu, "condenmErrorMargin"));

                    var realStart = Drawing.WorldToScreen(enemy.Position);
                    var realEnd = Drawing.WorldToScreen(condemnPos.To3D());

                    Drawing.DrawLine(realStart, realEnd, 2f, Color.Red);
                    new Circle() { Color = Color.Red, Radius = 60, BorderWidth = 2f }.Draw(condemnPos.To3D());
                }
            }

        }

        public static void OnAfterAttack(AttackableUnit target, EventArgs args)
        {
            if (target != null && (!target.IsValid || target.IsDead))
                return;

            var orbwalkermode = Orbwalker.ActiveModesFlags;

            if (orbwalkermode == Orbwalker.ActiveModes.Combo)
            {
                if (Misc.isChecked(ComboMenu, "comboQ") && Q.IsReady() && !Misc.isChecked(ComboMenu, "qsQUsage") && _Player.Distance(_target.Position) < (_Player.GetAutoAttackRange() + Q.Range))
                {
                    if (Misc.isChecked(ComboMenu, "qsQDirection"))
                    {
                        if (target != null) Q.Cast(target.Position);
                    }
                    else
                    {
                        Player.CastSpell(SpellSlot.Q, Game.CursorPos);
                    }
                }
            }

        }

        public static void OnBeforeAttack(AttackableUnit target, Orbwalker.PreAttackArgs args)
        {
            if (target != null && (!target.IsValid || target.IsDead))
                return;

            var orbwalkermode = Orbwalker.ActiveModesFlags;

            if (orbwalkermode == Orbwalker.ActiveModes.Combo)
            {
                if (Misc.isChecked(ComboMenu, "comboQ") && Q.IsReady() && Misc.isChecked(ComboMenu, "qsQUsage") && _Player.Distance(_target.Position) < (_Player.GetAutoAttackRange() + Q.Range))
                {
                    if (Misc.isChecked(ComboMenu, "qsQDirection"))
                    {
                        if (target != null) Q.Cast(target.Position);
                    }
                    else
                    {
                        Player.CastSpell(SpellSlot.Q, Game.CursorPos);
                    }
                }
            }
        }

        public static void OnProcessSpell(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe) return;
            if (args.SData.Name.ToLower().Contains("vaynetumble"))
            {
                Core.DelayAction(Orbwalker.ResetAutoAttack, 250);
            }
        }

        public static void OnGapCloser(AIHeroClient sender, Gapcloser.GapcloserEventArgs e)
        {
            if (sender == null || sender.IsAlly || !Misc.isChecked(CondemnMenu, "antiGapCloser")) return;

            if ((sender.IsAttackingPlayer || e.End.Distance(_Player) <= 70))
            {
                E.Cast(sender);
            }
        }

        public static void OnPossibleToInterrupt(Obj_AI_Base sender, Interrupter.InterruptableSpellEventArgs interruptableSpellEventArgs)
        {
            if (sender == null || sender.IsAlly || !Misc.isChecked(CondemnMenu, "interruptDangerousSpells")) return;

            if (interruptableSpellEventArgs.DangerLevel == DangerLevel.High && E.IsInRange(sender))
            {
                E.Cast(sender);
            }
        }

        public static void OnLasthit()
        {
            //  OnLasthit
        }

        public static void OnLaneClear()
        {

        }

        public static void OnHarass()
        {
            //  OnHarass
        }

        public static void OnCombo()
        {
            if (_target == null || !_target.IsValid)
                return;

            if (Misc.isChecked(ComboMenu, "forceSilverBolt"))
            {
                foreach (var enemy in ObjectManager.Get<AIHeroClient>().Where(t => t.IsEnemy).Where(t => _Player.GetAutoAttackRange() >= t.Distance(_Player)).Where(t => t.IsValidTarget()))
                {
                    if (enemy.PossibleDamage() >= enemy.Health)
                    {
                        _target = enemy;
                        Orbwalker.ForcedTarget = enemy;
                        break;
                    }
                    else
                    {
                        if (enemy.Has2WStacks())
                        {
                            _target = enemy;
                            Orbwalker.ForcedTarget = enemy;
                            break;
                        }
                    }
                }
            }

            if (Misc.isChecked(ComboMenu, "forceSilverBolt"))
            {
                foreach (var enemy in ObjectManager.Get<AIHeroClient>().Where(t => t.IsEnemy).Where(t => _Player.GetAutoAttackRange() >= t.Distance(_Player)).Where(t => t.IsValidTarget()))
                {
                    if (enemy.PossibleDamage() >= enemy.Health)
                    {
                        _target = enemy;
                        Orbwalker.ForcedTarget = enemy;
                        break;
                    }
                    else
                    {
                        if (enemy.Has2WStacks())
                        {
                            _target = enemy;
                            Orbwalker.ForcedTarget = enemy;
                            break;
                        }
                    }
                }
            }

            if (Misc.isChecked(ComboMenu, "comboR") && R.IsReady())
            {
                if (_Player.CountEnemiesInRange(_Player.GetAutoAttackRange()) >= Misc.getSliderValue(ComboMenu, "rsMinEnemiesForR"))
                {
                    R.Cast();
                }
            }
            if (Misc.isChecked(ComboMenu, "comboQ") && Q.IsReady())
            {
                if (Misc.isChecked(ComboMenu, "qsQOutAA") && _Player.Distance(_target.Position) < (_Player.GetAutoAttackRange() + Q.Range))
                {
                    if (Misc.isChecked(ComboMenu, "qsQDirection"))
                    {
                        Q.Cast(_target.Position);
                    }
                    else
                    {
                        Player.CastSpell(SpellSlot.Q, Game.CursorPos);
                    }
                }
            }

            if (Misc.isChecked(ComboMenu, "comboE") && E.IsReady())
            {
                AIHeroClient priorityTarget = null;
                foreach (var enemy in ObjectManager.Get<AIHeroClient>().Where(a => a.IsEnemy).Where(a => !a.IsDead).Where(a => E.IsInRange(a)))
                {
                    if (priorityTarget == null)
                    {
                        priorityTarget = enemy;
                    }
                    else
                    {
                        if (Misc.getSliderValue(CondemnMenu, "priorityCondemn" + enemy.ChampionName.ToLower()) > Misc.getSliderValue(CondemnMenu, "priorityCondemn" + priorityTarget.ChampionName.ToLower()))
                        {
                            priorityTarget = enemy;
                        }
                    }

                    if (!Misc.IsCondemnable(priorityTarget))
                        return;
                }

                if (priorityTarget != null && priorityTarget.IsValid && Misc.IsCondemnable(priorityTarget))
                {
                    E.Cast(priorityTarget);
                }
            }
        }

        public static void OnGameUpdate(EventArgs args)
        {
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
            {
                Orbwalker.ForcedTarget = null;
                OnLaneClear();
            }

            switch (Orbwalker.ActiveModesFlags)
            {
                case Orbwalker.ActiveModes.Combo:
                    /*if (Misc.isChecked(ComboMenu, "advTargetSelector"))
                    {
                        _target = Brain.Common.Selector.GetTarget(1100, DamageType.Physical, true);
                    }
                    else
                    {*/
                        _target = TargetSelector.GetTarget(1100, DamageType.Physical);
                    //}
                    OnCombo();
                    break;
                case Orbwalker.ActiveModes.LastHit:
                    Orbwalker.ForcedTarget = null;
                    OnLasthit();
                    break;
                case Orbwalker.ActiveModes.Harass:
                    Orbwalker.ForcedTarget = null;
                    OnHarass();
                    break;
            }
        }
    }
}
