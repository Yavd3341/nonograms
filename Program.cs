using JapaneseCrossword.UI;
using JapaneseCrossword.UI.Palettes;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace JapaneseCrossword {

	internal static class Program {

		public static MenuForm MenuForm { get; private set; }
		public static GameForm GameForm { get; private set; }

		public static IColorPalette Palette { get; private set; } = new BlueColorPalette();

		[STAThread]
		private static void Main() {
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			UIUtils.LoadBahnschrift();

			MenuForm = new MenuForm();
			GameForm = new GameForm();

			Application.Run(MenuForm);
		}

		public static double ToDistance(this Point a) => Math.Sqrt(a.X * a.X + a.Y * a.Y);
		public static Point Subtract(this Point a, Point b) => new Point(a.X - b.X, a.Y - b.Y);
		public static Point Add(this Point a, Point b) => new Point(a.X + b.X, a.Y + b.Y);
		public static Point Add(this Point a, Size b) => new Point(a.X + b.Width, a.Y + b.Height);
	}
}
