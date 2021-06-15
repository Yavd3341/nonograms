using System;
using System.Drawing;
using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace JapaneseCrossword.UI {
	public class Glyph {
		public PointF[] Path { get; set; }
		public int Size { get; set; }

		public Glyph(Point[] path, int size) {
			Path = Array.ConvertAll(path, (point) => new PointF(point.X, point.Y));
			Size = size;
		}

		public Glyph(PointF[] path, int size) {
			Path = path;
			Size = size;
		}
	}

	public class ColorUtils {
		// CodeProject: https://www.codeproject.com/Articles/16565/Determining-Ideal-Text-Color-Based-on-Specified-Ba
		public static Color IdealTextColor(Color bg) {
			int bgDelta = (int) ((bg.R * 0.299) + (bg.G * 0.587) + (bg.B * 0.114));
			return (255 - bgDelta < 105) ? Color.Black : Color.White;
		}

		public static Color ChangeOpacity(Color color, float opacity)
			=> Color.FromArgb((int) (0xFF * opacity), color.R, color.G, color.B);

		public static Color MixColors(Color color1, Color color2, float proportion) => Color.FromArgb(
				(int) (color1.A * proportion + color2.A * (1 - proportion)),
				(int) (color1.R * proportion + color2.R * (1 - proportion)),
				(int) (color1.G * proportion + color2.G * (1 - proportion)),
				(int) (color1.B * proportion + color2.B * (1 - proportion))
			);
	}

	public static class UIUtils {
		public static FontFamily Bahnschrift { get; private set; }
		public static void LoadBahnschrift() {
			try {
				Bahnschrift = new FontFamily("Bahnschrift");
			}
			catch (ArgumentException) {
				// StackOverflow: https://stackoverflow.com/a/6136417/8284672
				byte[] fontdata = Properties.Resources.Bahnschrift;
				IntPtr data = Marshal.AllocCoTaskMem(fontdata.Length);
				Marshal.Copy(fontdata, 0, data, fontdata.Length);
				PrivateFontCollection pfc = new PrivateFontCollection();
				pfc.AddMemoryFont(data, fontdata.Length);
				Bahnschrift = pfc.Families[0];
				Marshal.FreeCoTaskMem(data);
			}
		}
		public static string Inflate(this string text, int spacing)
			=> string.Join(new string((char) 0x2009, spacing), text.ToCharArray());
	}
}
