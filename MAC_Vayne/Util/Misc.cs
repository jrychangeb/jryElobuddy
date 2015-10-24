using System.Collections.Generic;
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
using System;
using MAC_Vayne.Plugin;

namespace MAC_Vayne.Util
{
    static class Misc
    {
        public static AIHeroClient _Player
        {
            get { return ObjectManager.Player; }
        }

        public static bool isChecked(Menu obj, String value)
        {
            return obj[value].Cast<CheckBox>().CurrentValue;
        }

        public static int getSliderValue(Menu obj, String value)
        {
            return obj[value].Cast<Slider>().CurrentValue;
        }

        public static bool Has2WStacks(this AIHeroClient target)
        {
            return target.Buffs.Any(bu => bu.Name == "vaynesilvereddebuff");
        }

        public static double PossibleDamage(this AIHeroClient target)
        {
            var damage = 0d;
            var targetMaxHealth = target.MaxHealth;

            var silverBoltDmg = (new float[] {0, 20, 30, 40, 50, 60}[Player.Instance.Spellbook.GetSpell(SpellSlot.W).Level] + new float[] { 0, targetMaxHealth / 4, targetMaxHealth / 5, targetMaxHealth / 6, targetMaxHealth / 7, targetMaxHealth / 8 }[Player.Instance.Spellbook.GetSpell(SpellSlot.W).Level]);

            if (Orbwalker.CanAutoAttack) damage += _Player.GetAutoAttackDamage(target, true);

            if (target.Has2WStacks()) damage += silverBoltDmg;

            return damage;
        }


        public static bool IsCondemnable(AIHeroClient target)
        {
            if (isChecked(Vayne.CondemnMenu, "dnCondemn" + target.ChampionName.ToLower()))
            {
                return false;
            }

            if (target.HasBuffOfType(BuffType.SpellImmunity) || target.HasBuffOfType(BuffType.SpellShield) || _Player.IsDashing()) return false;

            var position = Vayne._Player.Position.Extend(target.Position, Vayne._Player.Distance(target) - getSliderValue(Vayne.CondemnMenu, "condenmErrorMargin")).To3D();
            for (int i = 0; i < 470 - getSliderValue(Vayne.CondemnMenu, "condenmErrorMargin"); i += 10)
            {
                var cPos = _Player.Position.Extend(position, _Player.Distance(position) + i).To3D();
                if (cPos.ToNavMeshCell().CollFlags.HasFlag(CollisionFlags.Wall) || cPos.ToNavMeshCell().CollFlags.HasFlag(CollisionFlags.Building))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
