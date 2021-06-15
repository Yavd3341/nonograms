using JapaneseCrossword.UI.Palettes;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace JapaneseCrossword.UI.Controls {
	public class JCWWindowButton : JCWChainedButton, IPaletteReciever {
		protected new string Text => base.Text;
		protected new bool AutoSize => base.AutoSize;

		protected static readonly Glyph[] glyphs = {
			// Close
			new Glyph(new Point[] {
				new Point(2, 4),
				new Point(4, 2),
				new Point(9, 7),
				new Point(14, 2),
				new Point(16, 4),
				new Point(11, 9),
				new Point(16, 14),
				new Point(14, 16),
				new Point(9, 11),
				new Point(4, 16),
				new Point(2, 14),
				new Point(7, 9),
				new Point(2, 4)
			}, 18),

			// Maximize (to maximized state)
			new Glyph(new Point[] {
				new Point(2, 16),
				new Point(16, 16),
				new Point(16, 2),
				new Point(2, 2),
				new Point(2, 16),
				new Point(4, 14),
				new Point(14, 14),
				new Point(14, 6),
				new Point(4, 6),
				new Point(4, 14)
			}, 18),

			// Maximize (from maximized state)
			new Glyph(new Point[] {
				new Point(4, 2),
				new Point(4, 4),
				new Point(15, 4),
				new Point(15, 14),
				new Point(17, 14),
				new Point(17, 2),
				new Point(4, 2),
				new Point(2, 5),
				new Point(14, 5),
				new Point(14, 16),
				new Point(2, 16),
				new Point(2, 5),
				new Point(4, 9),
				new Point(12, 9),
				new Point(12, 14),
				new Point(4, 14),
				new Point(4, 9),
				new Point(2, 5),
			}, 18),

			// Minimize
			new Glyph(new Point[] {
				new Point(16, 16),
				new Point(2, 16),
				new Point(2, 14),
				new Point(16, 14),
				new Point(16, 16)
			}, 18)
		};

		private WindowButton buttonType = WindowButton.Close;

		IColorPalette IPaletteReciever.Palette {
			set {
				Palette = value;
				if (buttonType == WindowButton.Close) {
					IdleForeColor = value.CloseButtonIdleForeColor;
					HoverForeColor = value.CloseButtonHoverForeColor;
					DownForeColor = value.CloseButtonDownForeColor;
					DisabledForeColor = value.CloseButtonDisabledForeColor;
				}
			}
		}

		public new ChainDirection ChainingDirection {
			get => base.ChainingDirection;
			set {
				base.ChainingDirection = value;
				RecalculatePosition(true, true);
			}
		}

		public WindowButton ButtonType {
			get => buttonType;
			set {
				buttonType = value;
				Invalidate();
			}
		}

		public JCWWindowButton(IAnimator animator) : base(animator) => base.AutoSize = false;

		public void UpdateGlyph() =>
				Glyph = glyphs[buttonType == WindowButton.Maximize && FindForm()?.WindowState == FormWindowState.Maximized ? 2 : (int) ButtonType];

		public new void RecalculatePosition(bool updatePrev, bool updateNext) {
			int x = Location.X, y = Location.Y;
			Anchor = AnchorStyles.None;

			Form form = FindForm();

			if (ChainingDirection.HasFlag(ChainDirection.Down)) {
				Anchor |= AnchorStyles.Top;
				y = Margin.Top;
			}
			else if (ChainingDirection.HasFlag(ChainDirection.Up)) {
				Anchor |= AnchorStyles.Bottom;
				if (form != null)
					y = form.ClientSize.Height - Margin.Bottom - Height;
			}

			if (ChainingDirection.HasFlag(ChainDirection.Right)) {
				Anchor |= AnchorStyles.Left;
				x = Margin.Left;
			}
			else if (ChainingDirection.HasFlag(ChainDirection.Left)) {
				Anchor |= AnchorStyles.Right;
				if (form != null)
					x = form.ClientSize.Width - Margin.Right - Width;
			}

			Location = new Point(x, y);
			base.RecalculatePosition(updatePrev, updateNext);
		}

		protected override void OnPaint(PaintEventArgs e) {
			UpdateGlyph();
			base.OnPaint(e);
		}

		protected override void OnClick(EventArgs e) {
			base.OnClick(e);

			Form form = FindForm();

			if (buttonType == WindowButton.Close)
				form.Close();
			else
				form.WindowState = buttonType == WindowButton.Minimize
			  ? FormWindowState.Minimized
			  : form.WindowState == FormWindowState.Normal
			  ? FormWindowState.Maximized
			  : FormWindowState.Normal;
		}

		public enum WindowButton {
			Close = 0, Maximize = 1, Minimize = 3
		}
	}
}
