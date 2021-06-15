using JapaneseCrossword.UI.Palettes;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace JapaneseCrossword.UI.Controls {
	public class GameField : Panel, IPaletteReciever {
		public struct CellUpdateData {
			public int X { get; private set; }
			public int Y { get; private set; }
			public byte Color { get; private set; }

			public CellUpdateData(int x, int y, byte color) {
				X = x;
				Y = y;
				Color = color;
			}

			public override bool Equals(object obj) =>
				obj is CellUpdateData data &&
				data.X.Equals(X) &&
				data.Y.Equals(Y) &&
				data.Color.Equals(Color);

			public override int GetHashCode() => base.GetHashCode();
		}

		public class CellUpdateEventArgs : EventArgs {
			private CellUpdateData data;

			public bool Cancel { get; set; } = false;
			public int X => data.X;
			public int Y => data.Y;
			public byte Color => data.Color;
			public byte NewColor { get; private set; }

			public CellUpdateEventArgs(CellUpdateData data, byte newColor) {
				this.data = data;
				NewColor = newColor;
			}
		}

		private class ColorCount {
			public byte Color { get; set; }
			public int Count { get; set; }
			public bool Lock { get; set; }
		}

		public int RowCount { get; protected set; }
		public int ColumnCount { get; protected set; }

		private int supressionRows = 0;
		public int SupressionRows {
			get => supressionRows;
			protected set {
				if (value < 0)
					value = 0;

				supressionRows = value;
				RowCount = supressionRows + Crossword.Height;
				Height = RowCount * CellSide;
			}
		}

		private int supressionColumns = 0;
		public int SupressionColumns {
			get => supressionColumns;
			protected set {
				if (value < 0)
					value = 0;

				supressionColumns = value;
				ColumnCount = supressionColumns + Crossword.Width;
				Width = ColumnCount * CellSide;
			}
		}

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

		public float GridWidth { get; set; } = 1;
		public float BoldGridWidth { get; set; } = 3;
		public float BorderWidth { get; set; } = 3;

		public IColorPalette Palette {
			set {
				GridColor = value.MiscColors1;
				BoldGridColor = value.MiscColors2;
				BorderColor = value.MiscColors3;
			}
		}

		private Crossword crossword = new Crossword() {
			Palette = new Color[0],
			Field = new byte[0, 0]
		};
		public Crossword Crossword {
			get => crossword;
			set {
				crossword = (Crossword) value.Clone();
				crossword.Optimize();

				RowCount = crossword.Height;
				ColumnCount = crossword.Width;
				userField = new byte[ColumnCount, RowCount];
				lastActions.Clear();

				RebuildCache();
				Invalidate();
			}
		}

		protected byte[,] userField = new byte[0, 0];
		public byte this[int x, int y] => userField[x, y];
		protected Bitmap cachedGrid, cachedUserMap;

		protected Stack<CellUpdateData> lastActions = new Stack<CellUpdateData>();
		public int ActionsDone => lastActions.Count;

		public event EventHandler<CellUpdateEventArgs> CellUpdate;
		public event EventHandler LastActionsUpdate;

		public GameField() {
			DoubleBuffered = true;
			TabStop = true;
		}

		public void RebuildCache() {
			if (crossword.Height == 0 || crossword.Width == 0)
				return;

			Stack<ColorCount>[] rows = new Stack<ColorCount>[crossword.Height], cols = new Stack<ColorCount>[crossword.Width];
			int maxRow = 0, maxCol = 0;

			for (int y = 0; y < crossword.Height; y++) {
				for (int x = 0; x < crossword.Width; x++) {
					byte color = crossword.Field[x, y];
					if (rows[y] == null)
						rows[y] = new Stack<ColorCount>();
					if (rows[y].Count > 0 && !rows[y].Peek().Lock && rows[y].Peek().Color == color)
						rows[y].Peek().Count++;
					else {
						if (color == 0) {
							if (rows[y].Count > 0)
								rows[y].Peek().Lock = true;
						}
						else
							rows[y].Push(new ColorCount() { Color = color, Count = 1 });
					}
					if (rows[y].Count > rows[maxRow].Count)
						maxRow = y;

					if (cols[x] == null)
						cols[x] = new Stack<ColorCount>();
					if (cols[x].Count > 0 && !cols[x].Peek().Lock && cols[x].Peek().Color == color)
						cols[x].Peek().Count++;
					else {
						if (color == 0) {
							if (cols[x].Count > 0)
								cols[x].Peek().Lock = true;
						}
						else
							cols[x].Push(new ColorCount() { Color = color, Count = 1 });
					}
					if (cols[x].Count > cols[maxCol].Count)
						maxCol = x;
				}
			}

			rows[maxRow].TrimExcess();
			SupressionRows = cols[maxCol].Count;
			SupressionColumns = rows[maxRow].Count;

			cachedGrid?.Dispose();
			cachedGrid = new Bitmap(ColumnCount * CellSide, RowCount * CellSide);
			using (Graphics g = Graphics.FromImage(cachedGrid)) {
				using (Pen pen = new Pen(GridColor, GridWidth)) {
					for (int y = 0; y < crossword.Height; y++) {
						for (int x = SupressionColumns - 1; x >= 0 && rows[y].Count > 0; x--) {
							ColorCount cc = rows[y].Pop();

							using (Brush brush = new SolidBrush(crossword.Palette[cc.Color]))
								g.FillRectangle(brush, new Rectangle(x * CellSide, (y + SupressionRows) * CellSide, CellSide, CellSide));

							using (Brush brush = new SolidBrush(ColorUtils.IdealTextColor(crossword.Palette[cc.Color]))) {
								SizeF size = g.MeasureString(cc.Count.ToString(), Font);
								g.DrawString(cc.Count.ToString(), Font, brush,
									x * CellSide + (CellSide - size.Width) / 2,
									(y + SupressionRows) * CellSide + (CellSide - size.Height) / 2
								);
							}
						}
					}

					for (int x = 0; x < crossword.Width; x++) {
						for (int y = SupressionRows - 1; y >= 0 && cols[x].Count > 0; y--) {
							ColorCount cc = cols[x].Pop();

							using (Brush brush = new SolidBrush(crossword.Palette[cc.Color]))
								g.FillRectangle(brush, new Rectangle((x + SupressionColumns) * CellSide, y * CellSide, CellSide, CellSide));

							using (Brush brush = new SolidBrush(ColorUtils.IdealTextColor(crossword.Palette[cc.Color]))) {
								SizeF size = g.MeasureString(cc.Count.ToString(), Font);
								g.DrawString(
									cc.Count.ToString(), Font, brush,
										(x + SupressionColumns) * CellSide + (CellSide - size.Width) / 2,
										y * CellSide + (CellSide - size.Height) / 2
									);
							}
						}
					}

					for (int row = 0; row <= RowCount; row++)
						g.DrawLine(
							pen,
							row < SupressionRows ? SupressionColumns * CellSide : 0,
							row * CellSide,
							row > SupressionRows && (row - SupressionRows) % 5 == 0 ? SupressionColumns * CellSide : Width,
							row * CellSide
						);

					for (int col = 0; col <= ColumnCount; col++)
						g.DrawLine(
							pen,
							col * CellSide,
							col < SupressionColumns ? SupressionRows * CellSide : 0,
							col * CellSide,
							col > SupressionColumns && (col - SupressionColumns) % 5 == 0 ? SupressionRows * CellSide : Height
						);
				}

				using (Pen pen = new Pen(GridColor, BoldGridWidth)) {
					for (int row = SupressionRows + 5; row < RowCount; row += 5)
						g.DrawLine(pen, SupressionColumns * CellSide, row * CellSide, Width, row * CellSide);

					for (int col = SupressionColumns + 5; col < ColumnCount; col += 5)
						g.DrawLine(pen, col * CellSide, SupressionRows * CellSide, col * CellSide, Height);
				}

				using (Pen pen = new Pen(BoldGridColor, BoldGridWidth)) {
					g.DrawLine(pen, 0, SupressionRows * CellSide, Width, SupressionRows * CellSide);
					g.DrawLine(pen, SupressionColumns * CellSide, 0, SupressionColumns * CellSide, Height);
				}

				using (Pen pen = new Pen(BorderColor, BorderWidth))
					g.DrawRectangles(pen, new RectangleF[] {
						new RectangleF(
							SupressionColumns* CellSide,
							BorderWidth/2,
							Crossword.Width*CellSide - BorderWidth/2,
							Height - BorderWidth
						),

						new RectangleF(
							BorderWidth/2,
							SupressionRows * CellSide,
							Width - BorderWidth,
							Crossword.Height*CellSide - BorderWidth/2
						),
					});
			}

			cachedUserMap?.Dispose();
			cachedUserMap = new Bitmap(crossword.Width * CellSide, crossword.Height * CellSide);
			using (Graphics g = Graphics.FromImage(cachedUserMap)) {
				g.Clear(Color.White);

				for (int y = 0; y < Crossword.Height; y++) {
					Color color = Crossword.Palette[userField[0, y]];
					int colorSegment = 0;

					for (int x = 0; x < Crossword.Width; x++) {
						Color currentColor = Crossword.Palette[userField[x, y]];
						if (currentColor != color) {
							using (Brush brush = new SolidBrush(color))
								g.FillRectangle(
									brush,
									colorSegment * CellSide, y * CellSide,
									(x - colorSegment) * CellSide, CellSide
								);

							colorSegment = x;
							color = currentColor;
						}
					}

					using (Brush brush = new SolidBrush(color))
						g.FillRectangle(
							brush,
							colorSegment * CellSide, y * CellSide,
							(Crossword.Width - colorSegment) * CellSide, CellSide
						);
				}
			}
		}

		public void ShowSolution() {
			for (int y = 0; y < Crossword.Height; y++)
				for (int x = 0; x < Crossword.Width; x++)
					userField[x, y] = Crossword.Field[x, y];

			RebuildCache();
			Invalidate();
		}

		public void Undo() {
			if (ActionsDone > 0)
				UpdateCell(lastActions.Peek());
		}

		public void UpdateCell(CellUpdateData data) {
			int x = data.X, y = data.Y;
			CellUpdateData currentData = new CellUpdateData(x, y, userField[x, y]);
			if (!data.Equals(currentData)) {
				CellUpdateEventArgs args = new CellUpdateEventArgs(currentData, data.Color);
				CellUpdate?.Invoke(this, args);
				if (!args.Cancel) {
					userField[x, y] = data.Color;

					if (ActionsDone > 0 && lastActions.Peek().Equals(data))
						lastActions.Pop();
					else
						lastActions.Push(currentData);
					LastActionsUpdate?.Invoke(this, null);

					Rectangle invalidRect = new Rectangle(x * CellSide, y * CellSide, CellSide, CellSide);
					using (Graphics g = Graphics.FromImage(cachedUserMap))
					using (Brush brush = new SolidBrush(crossword.Palette[userField[x, y]]))
						g.FillRectangle(brush, invalidRect);

					Invalidate(new Rectangle((x + SupressionColumns) * CellSide, (y + SupressionRows) * CellSide, CellSide, CellSide));
				}
			}
			else if (ActionsDone > 0 && lastActions.Peek().Equals(data))
				lastActions.Pop();
		}

		protected override void OnPaint(PaintEventArgs e) {
			int ox = SupressionColumns * CellSide;
			int oy = SupressionRows * CellSide;

			int x = ox < e.ClipRectangle.X ? e.ClipRectangle.X : ox >= e.ClipRectangle.X + e.ClipRectangle.Width ? -1 : ox;
			int y = oy < e.ClipRectangle.Y ? e.ClipRectangle.Y : oy >= e.ClipRectangle.Y + e.ClipRectangle.Height ? -1 : oy;

			if (x != -1 && y != -1) {
				int w = e.ClipRectangle.X + e.ClipRectangle.Width - ox;
				int h = e.ClipRectangle.Y + e.ClipRectangle.Height - oy;

				e.Graphics.DrawImage(cachedUserMap, new Rectangle(x, y, w, h), new Rectangle(x - ox, y - oy, w, h), GraphicsUnit.Pixel);
			}

			e.Graphics.DrawImage(cachedGrid, e.ClipRectangle, e.ClipRectangle, GraphicsUnit.Pixel);
			base.OnPaint(e);
		}

		protected override void OnMouseClick(MouseEventArgs e) {
			base.OnMouseClick(e);

			if (e.Button == MouseButtons.Left) {
				int x = e.X / CellSide - SupressionColumns;
				int y = e.Y / CellSide - SupressionRows;

				if (x >= 0 && y >= 0)
					UpdateCell(new CellUpdateData(x, y, SelectedColor));
			}
		}

		protected override void Dispose(bool disposing) {
			cachedGrid?.Dispose();
			cachedUserMap?.Dispose();
			base.Dispose(disposing);
		}
	}
}
