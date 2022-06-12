using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KTXCore;
using KTXCore.Extensions;
using KTXCore.Graphics;
using Console = KTXCore.Console;

namespace Confight
{
    public abstract class SpellInfo
    {
        public int HManaCost { get; set; }
        public int CManaCost { get; set; }
        public int HealthCost { get; set; }
        public string Title { get; set; }

        public void Draw(int i, int j)
        {
            Console.SetCursorPosition(i, j);
            Console.Write(Title);
            if (HealthCost != 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write($" +{-HealthCost}".Replace("+-", "-"));
            }
            if (HManaCost != 0)
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.Write($" +{-HManaCost}".Replace("+-", "-"));
            }
            if (CManaCost != 0)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($" +{-CManaCost}".Replace("+-", "-"));
            }
            Console.ResetFore();
        }
    }

    public class Spell : SpellInfo
    {
        private Action<BattleContext> Invk;

        public byte ActiveKey;

        public Spell(int hm, int cm, int h, string title, byte key, Action<BattleContext> invoke)
        {
            (HManaCost, CManaCost, HealthCost, Title) = (hm, cm, h, title);
            ActiveKey = key;
            Invk = invoke;
        }

        public void Invoke(BattleContext context)
        {
            if (context.HMana >= HManaCost && context.CMana >= CManaCost && context.Health > HealthCost)
            {
                context.HMana -= HManaCost;
                context.CMana -= CManaCost;
                context.Health -= HealthCost;
                Invk.Invoke(context);
            }
            if (context.HMana > context.HManaMax) context.HMana = context.HManaMax;
            if (context.CMana > context.CManaMax) context.CMana = context.CManaMax;
            if (context.Health > context.HealthMax) context.Health = context.HealthMax;
        }
    }

    public class Endgame : Block
    {
        protected override ConsoleColor Background => ConsoleColor.Black;

        public BattleContext Context;

        public Endgame(BattleContext context)
        {
            Context = context;
            OnAllRedraw += AllRedraw;
            OnKeyPress += key => { if (key == Key.Spacebar) Close(); };
        }

        public void AllRedraw()
        {
            (Reference as Game).Context = new BattleContext();
            var rect = new Rectangle(50, 20, Alignment.CenterWidth | Alignment.CenterHeight);
            var (left, top) = rect.Draw();
            Console.SetCursorPosition(left + 2, top + 2);
            Console.Write("You are death!");
            Console.SetCursorPosition(left + 2, top + rect.Height - 3);
            Console.Write("Press SPACE to restart");
        }
    }

    public class Game : Block
    {
        public BattleContext Context;

        protected override ConsoleColor Background => ConsoleColor.Black;

        public ConsoleColor[,] LastDrawed;

        public bool Pause;

        public Game()
        {
            Console.Title = "Confight 1.0";
            Console.KeySplit = 200;
            Console.KeyWait = 1000;
            Console.OnlyOneRepeation = false;
            Console.Theme = Theme.Dark;
            Context = new BattleContext();
            Add(() => Context.Death, () => Start(new Endgame(Context)));
            Add(Up, Down);
            Add(() => !Pause, UpdateField);
            OnAllRedraw += AllRedraw;
            OnKeyPress += KeyPress;
        }

        public void AllRedraw()
        {
            LastDrawed = null;
            Console.SetCursorPosition(65, 7);
            var current = 7;
            foreach (var spell in Context.Spells)
                if (!string.IsNullOrEmpty(spell.Title))
                    spell.Draw(65, current++);
            Console.ResetFore();
        }

        const int offset_top = 7;
        const int offset_left = 1;

        public void UpdateField()
        {
            Context.Update();
            Console.SetCursorPosition(1, 1); Console.ForegroundColor = ConsoleColor.Red; Console.Write(Context.Health.ToString().PadRight(5));
            Graph.Bar(6, 1, Context.Health / (double)Context.HealthMax, ConsoleColor.Red, ConsoleColor.DarkRed, Console.FixedWindowWidth - 7);
            Console.SetCursorPosition(1, 2); Console.ForegroundColor = ConsoleColor.Magenta; Console.Write(Context.HMana.ToString().PadRight(5));
            Graph.Bar(6, 2, Context.HMana / (double)Context.HManaMax, ConsoleColor.Magenta, ConsoleColor.DarkMagenta, Console.FixedWindowWidth - 7);
            Console.SetCursorPosition(1, 3); Console.ForegroundColor = ConsoleColor.Cyan; Console.Write(Context.CMana.ToString().PadRight(5));
            Graph.Bar(6, 3, Context.CMana / (double)Context.CManaMax, ConsoleColor.Cyan, ConsoleColor.DarkCyan, Console.FixedWindowWidth - 7);
            if (LastDrawed == null)
            {
                LastDrawed = new ConsoleColor[Context.BattleField.GetLength(0), Context.BattleField.GetLength(1)];
                for (var i = 0; i < Context.BattleField.GetLength(0); i++)
                    for (var j = 0; j < Context.BattleField.GetLength(1); j++)
                    {
                        LastDrawed[i, j] = Context.GetColor(Context.BattleField[i, j]);
                        Console.SetCursorPosition(offset_left + i, offset_top + j);
                        Console.BackgroundColor = LastDrawed[i, j];
                        Graph.OutSpaces(1);
                    }
            }
            else
            {
                for (var i = 0; i < Context.BattleField.GetLength(0); i++)
                    for (var j = 0; j < Context.BattleField.GetLength(1); j++)
                    {
                        var gc = Context.GetColor(Context.BattleField[i, j]);
                        if (LastDrawed[i, j] != gc)
                        {
                            LastDrawed[i, j] = gc;
                            Console.SetCursorPosition(offset_left + i, offset_top + j);
                            Console.BackgroundColor = LastDrawed[i, j];
                            Graph.OutSpaces(1);
                        }
                    }
            }
            Console.ResetColor();
        }

        static void Main() => new Game().Start();

        void Up() => Context.MoveUp();
        void Down() => Context.MoveDown();

        void KeyPress(byte key)
        {
            var ind = Context.Spells.FindIndex(x => x.ActiveKey == key);
            if (ind != -1) Context.Spells[ind].Invoke(Context);

            KeypressInline(key, "Up", Key.W);
            KeypressInline(key, "Down", Key.S);
        }
    }
}
