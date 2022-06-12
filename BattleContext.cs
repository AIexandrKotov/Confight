using KTXCore.Graphics;
using KTXCore.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KTXCore;

namespace Confight
{
    public enum Celltype
    {
        Field,
        Player,

        Enemy,
        EnemyLazer,

        Dynenemy,

        Lazer,
    }

    public class BattleContext
    {
        public List<Spell> Spells = new List<Spell>();

        public ConsoleColor GetColor(Celltype celltype)
        {
            switch (celltype)
            {
                case Celltype.Player: return ConsoleColor.White;
                case Celltype.Field: return ConsoleColor.Black;
                case Celltype.Enemy: return ConsoleColor.DarkRed;
                case Celltype.EnemyLazer: return ConsoleColor.Red;
                case Celltype.Lazer: return ConsoleColor.Magenta;
                case Celltype.Dynenemy: return ConsoleColor.Green;
            }
            return ConsoleColor.Black;
        }

        public bool Death;

        public int HMana = 100, CMana = 1000, Health = 100;
        public int HManaMax = 1000, CManaMax = 1000, HealthMax = 1000;

        public Celltype[,] BattleField;

        public int PlayerPosition = 10;
        public int PlayerHeight = 1;

        public int MaxDown => BattleField.GetLength(1) - PlayerHeight;

        public void MoveDown()
        {
            if (PlayerPosition < MaxDown) PlayerPosition++;
        }

        public void MoveUp()
        {
            if (PlayerPosition > 0) PlayerPosition--;
        }

        int MinigunLast = 0;
        public void Minigun()
        {
            MinigunLast += 180;
        }

        public void AllLazer()
        {
            for (var i = 0; i < BattleField.GetLength(1); i++)
            {
                for (var j = 1; j <= 15; j++)
                {
                    BattleField[j, i] = Celltype.Lazer;
                }
            }
        }

        public void Lazer()
        {
            BattleField[1, PlayerPosition] = Celltype.Lazer;
            BattleField[2, PlayerPosition] = Celltype.Lazer;
            if (MinigunLast > 0) MinigunLast -= 1;
        }

        public void LongLazer()
        {
            BattleField[1, PlayerPosition] = Celltype.Lazer;
            BattleField[2, PlayerPosition] = Celltype.Lazer;
            BattleField[3, PlayerPosition] = Celltype.Lazer;
            BattleField[4, PlayerPosition] = Celltype.Lazer;
            BattleField[5, PlayerPosition] = Celltype.Lazer;
            BattleField[6, PlayerPosition] = Celltype.Lazer;
            BattleField[7, PlayerPosition] = Celltype.Lazer;
        }

        static Random rnd = new Random();
        long ticks = 0;
        int spawnrate = 120; //base 120
        int dsprate = 300;
        //int firerate = 60;
        int dyns = 0;

        public void Update()
        {
            if (MinigunLast > 0) Lazer();

            ticks++;
            HMana++;

            if (ticks % spawnrate == 0)
            {
                if (spawnrate > 3) spawnrate -= 2;
                BattleField[20 + rnd.Next(BattleField.GetLength(0)) - 20, rnd.Next(BattleField.GetLength(1))] = Celltype.Enemy;
            }

            if (spawnrate < 60)
            {
                if (ticks % dsprate == 0)
                {
                    dyns += 1;
                    BattleField[50, 10] = Celltype.Dynenemy;
                    if (dsprate > 60) dsprate -= 10;
                    for (var j = 0; j < 10; j++)
                    {
                        for (var i = 1; i < 10; i++)
                        {
                            if (dyns > j * 10 + i - 1)
                            {
                                BattleField[50 - j, 10 - i] = Celltype.Dynenemy;
                                BattleField[50 - j, 10] = Celltype.Dynenemy;
                                BattleField[50 - j, 10 + i] = Celltype.Dynenemy;
                            }
                        }
                    }
                }
            }

            var width = BattleField.GetLength(0);
            var height = BattleField.GetLength(1);
            for (var i = 0; i < width; i++)
                for (var j = 0; j < height; j++)
                {
                    if (BattleField[i, j] == Celltype.Dynenemy)
                    {
                        if (i > 1)
                        {
                            BattleField[i - 1, j] = Celltype.Dynenemy;
                            BattleField[i, j] = Celltype.Field;
                        }
                        else Health -= 25;
                    }

                    if (BattleField[i, j] == Celltype.EnemyLazer)
                    {
                        if (i > 1)
                        {
                            switch (BattleField[i - 1, j])
                            {
                                case Celltype.Field:
                                    BattleField[i - 1, j] = Celltype.EnemyLazer;
                                    break;
                                case Celltype.Lazer:
                                    BattleField[i - 1, j] = Celltype.Field;
                                    CMana += 2;
                                    break;
                            }
                        }
                        else if (j == PlayerPosition) CMana += 1;
                        else Health -= 5;
                        BattleField[i, j] = Celltype.Field;
                    }

                    if (BattleField[i, j] == Celltype.Enemy && i > 0)
                    {
                        if (rnd.Chance(0.01)) BattleField[i - 1, j] = Celltype.EnemyLazer;
                    }
                }

            for (var i = width - 1; i >= 0; i--)
            {
                for (var j = height - 1; j >= 0; j--)
                {
                    if (BattleField[i, j] == Celltype.Lazer)
                    {
                        BattleField[i, j] = Celltype.Field;
                        if (i + 1 < width)
                        {
                            if (BattleField[i + 1, j] == Celltype.Enemy) CMana += 10;
                            BattleField[i + 1, j] = Celltype.Lazer;
                        }
                    }
                }
            }

            for (var i = 0; i < BattleField.GetLength(1); i++)
            {
                if (i >= PlayerPosition && i < PlayerPosition + PlayerHeight) BattleField[0, i] = Celltype.Player;
                else BattleField[0, i] = Celltype.Field;
            }

            if (HMana > HManaMax) HMana = HManaMax;
            if (CMana > CManaMax) CMana = CManaMax;
            if (Health > HealthMax) Health = HealthMax;
            if (Health <= 0)
            {
                Death = true;
                Health = 0;
            }
        }

        public BattleContext()
        {
            BattleField = new Celltype[60, 20];
            Spells.Add(new Spell(15, 0, 0, "(D) Lazer", Key.D, context => context.Lazer()));
            Spells.Add(new Spell(15, 0, 0, "", 1, context => context.Lazer()));
            Spells.Add(new Spell(70, 0, 0, "(F) Long Lazer", Key.F, context => context.LongLazer()));
            Spells.Add(new Spell(100, 0, -35, "(Q) Heal", Key.Q, context => { }));

            //Spells.Add(new Spell(-1000, 100, 0, "(X) Fill", Key.X, context => { }));
            Spells.Add(new Spell(0, 100, -150, "(A) Cold Heal", Key.A, context => { }));
            Spells.Add(new Spell(0, 150, 0, "(R) Minigun", Key.R, context => context.Minigun()));
            Spells.Add(new Spell(0, 200, 0, "(G) Ultimate", Key.G, context => context.AllLazer()));
        }
    }
}
