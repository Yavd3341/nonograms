using JapaneseCrossword.UI.Palettes;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace JapaneseCrossword.UI.Controls {
	public class EditorPane : Panel, IPaletteReciever {
		public int RowCount { get; protected set; }
		public int ColumnCount { get; protected set; }

		private int cellSide = 10;
		public int CellSide {
			get => cellSide;
			set {
				if (value < 1)
					value = 1;

				cellSide = value;
				RebuildCache();
				Invalidate();
			}
		}

		public byte SelectedColor { get; set; } = 0;

		public Color GridColor { get; set; }
		public Color BoldGridColor { get; set; }
		public Color BorderColor { get; set; }
		public Color SelectionColor { get; set; }

		public float GridWidth { get; set; } = 1;
		public float BoldGridWidth { get; set; } = 3;
		public float BorderWidth { get; set; } = 3;

		public IColorPalette Palette {
			set {
				GridColor = value.MiscColors1;
				BoldGridColor = value.MiscColors2;
				BorderColor = value.MiscColors3;
				SelectionColor = value.MiscColors4;

				RebuildCache();
			}
		}

		public Color[] ColorPalette { get; set; }
		public byte[,] Field { get; set; } = new byte[0, 0];
		protected Bitmap cachedGrid, cachedUserMap;

		public event EventHandler DataChanged;

		public EditorPane() {
			DoubleBuffered = true;
			ColorPalette = new Color[1] { Color.White };
		}

		public void NewImage(Size size) {
			RowCount = size.Height;
			ColumnCount = size.Width;

			Width = ColumnCount * CellSide;
			Height = RowCount * CellSide;

			Field = new byte[ColumnCount, RowCount];

			RebuildCache();
			Invalidate();
		}

		public void RebuildCache() {
			cachedGrid?.Dispose();
			cachedGrid = new Bitmap(Width, Height);
			using (Graphics g = Graphics.FromImage(cachedGrid)) {
				using (Pen pen = new Pen(GridColor, GridWidth)) {
					for (int row = 0; row < RowCount; row++)
						g.DrawLine(pen, 0, row * CellSide, Width, row * CellSide);

					for (int col = 0; col < ColumnCount; col++)
						g.DrawLine(pen, col * CellSide, 0, col * CellSide, Height);
				}

				using (Pen pen = new Pen(GridColor, BoldGridWidth)) {
					for (int row = 5; row < RowCount; row += 5)
						g.DrawLine(pen, 0, row * CellSide, Width, row * CellSide);

					for (int col = 5; col < ColumnCount; col += 5)
						g.DrawLine(pen, col * CellSide, 0, col * CellSide, Height);
				}

				using (Pen pen = new Pen(BorderColor, BorderWidth))
					g.DrawRectangle(pen, BorderWidth / 2, BorderWidth / 2, ColumnCount * CellSide - BorderWidth, RowCount * CellSide - BorderWidth);
			}

			cachedUserMap?.Dispose();
			cachedUserMap = new Bitmap(Width, Height);
			using (Graphics g = Graphics.FromImage(cachedUserMap))
				g.Clear(Color.White);
		}

		public void UpdateCell(int x, int y, byte color) {
			if (Field[x, y] != color) {
				Field[x, y] = color;
				DataChanged?.Invoke(this, null);

				Rectangle invalidRect = new Rectangle(x * CellSide, y * CellSide, CellSide, CellSide);
				using (Graphics g = Graphics.FromImage(cachedUserMap))
				using (Brush brush = new SolidBrush(ColorPalette[Field[x, y]]))
					g.FillRectangle(brush, invalidRect);

				Invalidate(new Rectangle(x * CellSide, y * CellSide, CellSide, CellSide));
			}
		}

		protected override void OnPaint(PaintEventArgs e) {
			e.Graphics.DrawImage(cachedUserMap, e.ClipRectangle, e.ClipRectangle, GraphicsUnit.Pixel);
			e.Graphics.DrawImage(cachedGrid, e.ClipRectangle, e.ClipRectangle, GraphicsUnit.Pixel);
			base.OnPaint(e);
		}

		protected override void OnMouseClick(MouseEventArgs e) {
			base.OnMouseClick(e);

			if (e.Button == MouseButtons.Left) {
				int x = e.X / CellSide;
				int y = e.Y / CellSide;

				if (x >= 0 && y >= 0)
					UpdateCell(x, y, SelectedColor);
			}
		}

		protected override void Dispose(bool disposing) {
			cachedGrid.Dispose();
			cachedUserMap.Dispose();
			base.Dispose(disposing);
		}
	}
}
