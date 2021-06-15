using JapaneseCrossword.UI.Palettes;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace JapaneseCrossword.UI.Controls {
	public class JCWForm : Form, IAnimator, IPaletteReciever {
		[Flags]
		private enum DimensionsUpdateState {
			None = 0, ResizeX = 1, ResizeY = 2, UpdateLocationX = 4, UpdateLocationY = 8
		}

		[Flags]
		public enum ResizableDimensions {
			None = 0, ResizeX = 1, ResizeY = 2, All = ResizeX | ResizeY
		}

		protected const int borderSize = 8;
		protected const int headerSize = 32;

		private Point mouseOrigin;
		private DimensionsUpdateState dus;

		private bool directControls;

		protected Timer animationTimer;
		protected uint animationSubscribers = 0;
		protected JCWWindowButton closeButton, maxButton, minButton;
		protected Panel content;

		protected Control.ControlCollection BaseControls => base.Controls;
		protected bool UseDirectControls {
			get => directControls;
			set {
				directControls = value;
				if (!value && (content == null || content.IsDisposed)) {
					if (content != null)
						base.Controls.Remove(content);

					content = new Panel() {
						Location = new Point(borderSize, headerSize + 1),
						Size = new Size(Width - borderSize * 2, Height - borderSize - headerSize - 1),
						Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
						AutoScroll = true,
						Capture = false,
					};

					base.Controls.Add(content);
				}
			}
		}

		public new Cursor Cursor { get; set; } = Cursors.Arrow;
		public new Color BackColor {
			get => base.BackColor;
			set => base.BackColor = value;
		}

		public Color HeaderBackColor { get; set; }
		public ResizableDimensions CanResize { get; set; } = ResizableDimensions.All;

		public IColorPalette Palette {
			set {
				ForeColor = value.FormForeColor;
				BackColor = value.FormBackColor;
				HeaderBackColor = value.FormHeaderColor;

				foreach (Control control in base.Controls)
					if (control is IPaletteReciever paletteReciever)
						paletteReciever.Palette = value;

				if (!UseDirectControls)
					foreach (Control control in Controls)
						if (control is IPaletteReciever paletteReciever)
							paletteReciever.Palette = value;
			}
		}

		public new Size ClientSize {
			get => UseDirectControls ? Size : content.Size;
			set => Size = UseDirectControls ? value : new Size(value.Width + borderSize * 2, value.Height + borderSize + headerSize + 1);
		}

		public new FormBorderStyle FormBorderStyle => base.FormBorderStyle;
		public new Control.ControlCollection Controls => UseDirectControls ? base.Controls : content.Controls;
		public new bool MaximizeBox {
			get => base.MaximizeBox;
			set {
				base.MaximizeBox = value;
				maxButton.Enabled = value;
				maxButton.Visible = value;
				minButton.ChainedControl = value ? maxButton : closeButton;
			}
		}
		public new bool MinimizeBox {
			get => base.MinimizeBox;
			set {
				base.MinimizeBox = value;
				minButton.Enabled = value;
				minButton.Visible = value;
			}
		}
		public new bool AutoScroll {
			get => content.AutoScroll;
			set => content.AutoScroll = value;
		}

		public event EventHandler AnimationTick {
			add {
				animationTimer.Tick += value;
				animationSubscribers++;
				if (!animationTimer.Enabled)
					animationTimer.Start();
			}
			remove {
				animationTimer.Tick -= value;
				animationSubscribers--;
				if (animationSubscribers == 0)
					animationTimer.Stop();
			}
		}

		public JCWForm() : base() {
			base.FormBorderStyle = FormBorderStyle.None;
			MinimumSize = new Size(64, 64);
			Size = new Size(500, 500);
			ResizeRedraw = true;
			UseDirectControls = false;
			DoubleBuffered = true;

			animationTimer = new Timer() { Interval = 1 };

			JCWWindowButton GetWindowButton(JCWWindowButton.WindowButton type, JCWWindowButton prev, JCWChainedButton.ChainDirection cd) => new JCWWindowButton(this) {
				Size = new Size() { Width = headerSize, Height = headerSize },
				ButtonType = type,
				Margin = new Padding(0),
				Padding = new Padding(8),
				ChainedControl = prev,
				ChainingDirection = cd
			};

			closeButton = GetWindowButton(
				JCWWindowButton.WindowButton.Close,
				null,
				JCWChainedButton.ChainDirection.Left | JCWChainedButton.ChainDirection.Down
			);
			closeButton.Margin = new Padding(1);

			maxButton = GetWindowButton(
				JCWWindowButton.WindowButton.Maximize,
				closeButton,
				JCWChainedButton.ChainDirection.Left
			);

			minButton = GetWindowButton(
				JCWWindowButton.WindowButton.Minimize,
				maxButton,
				JCWChainedButton.ChainDirection.Left
			);

			base.Controls.Add(closeButton);
			base.Controls.Add(maxButton);
			base.Controls.Add(minButton);

			closeButton.RecalculatePosition(true, true);
		}

		protected override void OnMouseDoubleClick(MouseEventArgs e) {
			base.OnMouseDoubleClick(e);

			if (CanResize == ResizableDimensions.All && e.Y > borderSize && e.Y <= headerSize) {
				WindowState = WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized;
				maxButton.Invalidate();
			}

		}

		protected override void OnMouseLeave(EventArgs e) {
			base.OnMouseLeave(e);
			base.Cursor = Cursor;
		}

		protected override void OnMouseDown(MouseEventArgs e) {
			base.OnMouseDown(e);
			if (e.Button == MouseButtons.Left) {
				mouseOrigin = MousePosition;

				DimensionsUpdateState dus = DimensionsUpdateState.None;
				if (CanResize.HasFlag(ResizableDimensions.ResizeX) && (e.X < borderSize || e.X > Width - borderSize))
					dus |= DimensionsUpdateState.ResizeX;

				if (CanResize.HasFlag(ResizableDimensions.ResizeY) && (WindowState != FormWindowState.Maximized && e.Y < borderSize || e.Y > Height - borderSize))
					dus |= DimensionsUpdateState.ResizeY;

				if (e.X < borderSize || (WindowState == FormWindowState.Maximized || e.Y > borderSize) && e.Y < headerSize)
					dus |= DimensionsUpdateState.UpdateLocationX;

				if (e.Y < headerSize)
					dus |= DimensionsUpdateState.UpdateLocationY;

				this.dus = dus;
			}
		}

		protected override void OnMouseUp(MouseEventArgs e) {
			base.OnMouseUp(e);
			if (e.Button == MouseButtons.Left && dus != DimensionsUpdateState.None) {
				dus = DimensionsUpdateState.None;
				if (Location.Y < 0)
					Location = new Point(Location.X, 0);
			}
		}

		protected override void OnMouseMove(MouseEventArgs e) {
			base.OnMouseMove(e);

			Point localMouse = MousePosition.Subtract(Location);
			Point mouseDelta = MousePosition.Subtract(mouseOrigin);

			if ((dus.HasFlag(DimensionsUpdateState.UpdateLocationX)
				&& dus.HasFlag(DimensionsUpdateState.UpdateLocationY))
				&& WindowState == FormWindowState.Maximized
				&& mouseDelta.ToDistance() > borderSize * 2) {

				int oldLocalMouseY = mouseOrigin.Y - Location.Y;
				if (oldLocalMouseY < borderSize)
					oldLocalMouseY = borderSize;

				WindowState = FormWindowState.Normal;
				Location = new Point(MousePosition.X - Width / 2, MousePosition.Y - oldLocalMouseY);
				maxButton.Invalidate();

				mouseOrigin = MousePosition;
				return;
			}

			if (WindowState != FormWindowState.Maximized) {
				base.Cursor = // This was proposed by Visual Studio as replacement for if-elseif-else ladder and I think I like it
					  CanResize.HasFlag(ResizableDimensions.All) && (localMouse.X < borderSize && localMouse.Y < borderSize || localMouse.X > Width - borderSize && localMouse.Y > Height - borderSize)
					? Cursors.SizeNWSE
					: CanResize.HasFlag(ResizableDimensions.All) && (localMouse.X > Width - borderSize && localMouse.Y < borderSize || localMouse.X < borderSize && localMouse.Y > Height - borderSize)
					? Cursors.SizeNESW
					: CanResize.HasFlag(ResizableDimensions.ResizeX) && (localMouse.X < borderSize || localMouse.X > Width - borderSize)
					? Cursors.SizeWE
					: CanResize.HasFlag(ResizableDimensions.ResizeY) && (localMouse.Y < borderSize || localMouse.Y > Height - borderSize)
					? Cursors.SizeNS
					: Cursor;

				if (e.Button == MouseButtons.Left && dus != DimensionsUpdateState.None) {
					int w = Size.Width, h = Size.Height, x = Location.X, y = Location.Y;
					mouseOrigin = MousePosition;

					if (dus.HasFlag(DimensionsUpdateState.UpdateLocationX))
						x += mouseDelta.X;

					if (dus.HasFlag(DimensionsUpdateState.UpdateLocationY))
						y += mouseDelta.Y;

					if (dus.HasFlag(DimensionsUpdateState.ResizeX))
						w += (dus.HasFlag(DimensionsUpdateState.UpdateLocationX) ? -1 : 1) * mouseDelta.X;

					if (dus.HasFlag(DimensionsUpdateState.ResizeY))
						h += (dus.HasFlag(DimensionsUpdateState.UpdateLocationY) ? -1 : 1) * mouseDelta.Y;

					SetBounds(x, y, w, h, BoundsSpecified.All);
				}
			}
		}

		protected override void OnPaint(PaintEventArgs e) {
			base.OnPaint(e);
			e.Graphics.Clear(BackColor);

			using (Brush brush = new SolidBrush(HeaderBackColor))
				e.Graphics.FillPolygon(brush, new Point[] {
					new Point(0,0),
					new Point(Width,0),
					new Point(Width,Height),
					new Point(0,Height),
					new Point(0,0),

					new Point(borderSize,headerSize+1),
					new Point(Width-borderSize,headerSize+1),
					new Point(Width-borderSize,Height-borderSize),
					new Point(borderSize,Height-borderSize),
					new Point(borderSize,headerSize+1)
				});

			SizeF size = e.Graphics.MeasureString(Text, Font);
			using (Brush brush = new SolidBrush(ForeColor))
				e.Graphics.DrawString(Text, Font, brush, borderSize, (headerSize - size.Height) / 2);

			using (Pen pen = new Pen(ForeColor))
				e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
		}
	}
}
