using JapaneseCrossword.UI.Palettes;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace JapaneseCrossword.UI.Controls {
	public class PaletteControl : Control, IPaletteReciever {
		private int rowCount = 5;
		public int RowCount {
			get => rowCount;
			set {
				if (value > 0)
					rowCount = value;
				UpdateSize();
			}
		}

		private int columnCount = 5;
		public int ColumnCount {
			get => columnCount;
			set {
				if (value > 0)
					columnCount = value;
				UpdateSize();
			}
		}

		private int page;

		public new int Width => base.Width;
		public new int Height => base.Height;

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

		public bool SkipFirst { get; set; } = true;
		public byte SelectedColor { get; protected set; } = 0;
		protected Point SelectedCell { get; set; } = new Point(0, 0);

		protected JCWChainedButton nextPageBtn, prevPageBtn;

		public Color GridColor { get; set; }
		public Color BoldGridColor { get; set; }
		public Color BorderColor { get; set; }
		public Color SelectionColor { get; set; }

		public IColorPalette Palette {
			set {
				GridColor = value.MiscColors1;
				BoldGridColor = value.MiscColors2;
				BorderColor = value.MiscColors3;
				SelectionColor = value.MiscColors4;

				nextPageBtn.Palette = value;
				prevPageBtn.Palette = value;

				RebuildCache();
			}
		}

		public float GridWidth { get; set; } = 1;
		public float BorderWidth { get; set; } = 3;
		public float SelectionWidth { get; set; } = 3;

		protected Bitmap cache;
		protected Color[] palette;
		public Color[] ColorPalette {
			get {
				Color[] result = new Color[palette.Length];
				Array.Copy(palette, result, palette.Length);
				return result;
			}
			set {
				palette = new Color[value.Length];
				Array.Copy(value, palette, value.Length);
				UpdateSize();
				ChangePage(0);
			}
		}

		public event EventHandler ColorChange;

		public PaletteControl(IAnimator animator) {
			nextPageBtn = new JCWChainedButton(animator) {
				Glyph = new Glyph(new Point[] {
					new Point(0, 3),
					new Point(1, 3),
					new Point(2, 2),
					new Point(3, 3),
					new Point(4, 3),
					new Point(2, 1),
				}, 4),
				Enabled = false,
				Visible = false
			};
			nextPageBtn.Click += (sender, e) => {
				ChangePage(page + 1);
				UpdateButtons();
			};
			Controls.Add(nextPageBtn);

			prevPageBtn = new JCWChainedButton(animator) {
				Glyph = new Glyph(new Point[] {
					new Point(0, 1),
					new Point(1, 1),
					new Point(2, 2),
					new Point(3, 1),
					new Point(4, 1),
					new Point(2, 3)
				}, 4),
				Enabled = false,
				Visible = false
			};
			prevPageBtn.Click += (sender, e) => {
				ChangePage(page - 1);
				UpdateButtons();
			};
			Controls.Add(prevPageBtn);
		}

		protected void UpdateButtons() {
			nextPageBtn.Enabled = page < Math.Ceiling((float) palette.Length / (columnCount * rowCount)) - 1;
			prevPageBtn.Enabled = page > 0;
		}

		public void UpdateSize() {
			base.Width = columnCount * CellSide;
			base.Height = rowCount * CellSide;

			if (palette != null && palette.Length > columnCount * rowCount) {
				base.Height += 35;

				nextPageBtn.Size = new Size(CellSide * columnCount / 2 - 5, 25);
				nextPageBtn.Location = new Point(0, CellSide * rowCount + 10);
				nextPageBtn.Visible = true;

				prevPageBtn.Size = nextPageBtn.Size;
				prevPageBtn.Location = new Point(nextPageBtn.Right + 10, CellSide * rowCount + 10);
				prevPageBtn.Visible = true;
			}
			else {
				nextPageBtn.Visible = false;
				prevPageBtn.Visible = false;
			}

			RebuildCache();
		}

		public void RebuildCache() => ChangePage(page);
		public void ChangePage(int page) {
			if (palette == null)
				return;

			cache?.Dispose();
			cache = new Bitmap(ColumnCount * CellSide, RowCount * CellSide);
			using (Graphics g = Graphics.FromImage(cache)) {
				g.Clear(Color.Transparent);

				for (int i = 0; i < columnCount * rowCount; i++) {
					if (i + page * columnCount * rowCount < palette.Length)
						using (Brush brush = new SolidBrush(palette[i + page * columnCount * rowCount]))
							g.FillRectangle(brush, i % columnCount * CellSide, i / columnCount * CellSide, CellSide, CellSide);
					else
						break;
				}

				using (Pen pen = new Pen(GridColor, GridWidth)) {
					for (int row = 0; row < RowCount; row++)
						g.DrawLine(pen, 0, row * CellSide, Width, row * CellSide);

					for (int col = 0; col < ColumnCount; col++)
						g.DrawLine(pen, col * CellSide, 0, col * CellSide, Height);
				}

				using (Pen pen = new Pen(BorderColor, BorderWidth))
					g.DrawRectangle(pen, BorderWidth / 2, BorderWidth / 2, ColumnCount * CellSide - BorderWidth, RowCount * CellSide - BorderWidth);
			}

			Invalidate();

			this.page = page;
			SelectCell(SkipFirst && page == 0 && palette.Length > 1 ? 1 : 0, 0);
			UpdateButtons();
		}

		public void SelectCell(int x, int y) {
			if (x + (y + page * rowCount) * columnCount >= palette.Length || x >= rowCount || y >= columnCount)
				return;

			SelectedColor = (byte) ((page * rowCount + y) * columnCount + x);
			ColorChange?.Invoke(this, null);

			Point oldSelectedCell = SelectedCell;
			SelectedCell = new Point(x, y);

			if (oldSelectedCell.Equals(SelectedCell))
				return;

			if (oldSelectedCell.X >= 0 && oldSelectedCell.Y >= 0)
				Invalidate(new Region(new RectangleF(
					CellSide * oldSelectedCell.X - SelectionWidth / 2,
					CellSide * oldSelectedCell.Y - SelectionWidth / 2,
					CellSide + SelectionWidth,
					CellSide + SelectionWidth
				)));

			Invalidate(new Region(new RectangleF(
				CellSide * SelectedCell.X - SelectionWidth / 2,
				CellSide * SelectedCell.Y - SelectionWidth / 2,
				CellSide + SelectionWidth,
				CellSide + SelectionWidth
			)));
		}

		protected override void OnPaint(PaintEventArgs e) {
			e.Graphics.DrawImage(cache, e.ClipRectangle, e.ClipRectangle, GraphicsUnit.Pixel);
			e.Graphics.DrawRectangle(new Pen(SelectionColor, SelectionWidth), CellSide * SelectedCell.X + SelectionWidth / 2, CellSide * SelectedCell.Y + SelectionWidth / 2, CellSide - SelectionWidth, CellSide - SelectionWidth);
			base.OnPaint(e);
		}

		protected override void OnMouseClick(MouseEventArgs e) {
			base.OnMouseClick(e);

			if (e.Button == MouseButtons.Left) {
				int x = e.X / CellSide;
				int y = e.Y / CellSide;
				SelectCell(x, y);
			}

		}

		protected override void Dispose(bool disposing) {
			cache?.Dispose();
			base.Dispose(disposing);
		}
	}
}
