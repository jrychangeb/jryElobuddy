using EloBuddy;
using EloBuddy.SDK.Events;
using MAC_Vayne.Plugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MAC_Vayne
{
    class Program
    {
        static void Main(string[] args)
        {
            Loading.OnLoadingComplete += Game_OnStart;
        }

        private static void Game_OnStart(EventArgs args)
        {
            var champion = ObjectManager.Player.ChampionName.ToLower();

            switch (champion)
            {
                case "vayne":
                    Vayne.Init();
                    break;
            }

        }
    }
}
