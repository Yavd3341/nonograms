using JapaneseCrossword.UI.Palettes;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace JapaneseCrossword.UI.Controls {
	public class JCWButton : Button, IPaletteReciever {
		public Color IdleForeColor { get; set; }
		public Color IdleBackColor { get; set; }
		public Color HoverForeColor { get; set; }
		public Color HoverBackColor { get; set; }
		public Color DownForeColor { get; set; }
		public Color DownBackColor { get; set; }
		public Color DisabledForeColor { get; set; }
		public Color DisabledBackColor { get; set; }

		public new Color ForeColor {
			get => IdleForeColor;
			set => IdleForeColor = value;
		}

		public new Color BackColor {
			get => IdleBackColor;
			set => IdleBackColor = value;
		}

		public IColorPalette Palette {
			set {
				IdleForeColor = value.ButtonIdleForeColor;
				IdleBackColor = value.ButtonIdleBackColor;
				HoverForeColor = value.ButtonHoverForeColor;
				HoverBackColor = value.ButtonHoverBackColor;
				DownForeColor = value.ButtonDownForeColor;
				DownBackColor = value.ButtonDownBackColor;
				DisabledForeColor = value.ButtonDisabledForeColor;
				DisabledBackColor = value.ButtonDisabledBackColor;
			}
		}

		public Glyph Glyph { get; set; }

		protected IAnimator animator;
		protected float animationProgress = 0;
		protected AnimationTarget animationTarget = AnimationTarget.Idle;
		public float AnimationStep { get; set; } = 0.075f;

		public JCWButton(IAnimator animator) {
			this.animator = animator;
			DoubleBuffered = true;
		}

		private void OnAnimationTick(object sender, EventArgs e) {
			if (animationTarget == AnimationTarget.MouseDown)
				return;
			else if (animationTarget == AnimationTarget.Hover) {
				animationProgress += AnimationStep;
				if (animationProgress >= 1) {
					animationProgress = 1;
					animationTarget = AnimationTarget.None;
				}
			}
			else if (animationTarget == AnimationTarget.Idle) {
				animationProgress -= AnimationStep;
				if (animationProgress <= 0) {
					animationProgress = 0;
					animationTarget = AnimationTarget.None;
				}
			}
			else {
				animator.AnimationTick -= OnAnimationTick;
			}

			Invalidate();
		}

		protected override void OnInvalidated(InvalidateEventArgs e) {
			base.OnInvalidated(e);
			if (RectangleToScreen(ClientRectangle).Contains(MousePosition)) {
				if (animationTarget != AnimationTarget.Hover && animationProgress > 1) {
					animationTarget = AnimationTarget.Hover;
					animationProgress = 0;
					animator.AnimationTick += OnAnimationTick;
				}
			}
			else {
				if (animationTarget != AnimationTarget.Idle && animationProgress > 0) {
					animationTarget = AnimationTarget.Idle;
					animationProgress = 1;
					animator.AnimationTick += OnAnimationTick;
				}
			}
		}

		protected override void OnMouseEnter(EventArgs e) {
			base.OnMouseEnter(e);
			animationTarget = AnimationTarget.Hover;
			animator.AnimationTick += OnAnimationTick;
		}

		protected override void OnMouseLeave(EventArgs e) {
			base.OnMouseLeave(e);
			animationTarget = AnimationTarget.Idle;
			animator.AnimationTick += OnAnimationTick;
		}

		protected override void OnMouseDown(MouseEventArgs mevent) {
			base.OnMouseDown(mevent);
			animationTarget = AnimationTarget.MouseDown;
		}

		protected override void OnMouseUp(MouseEventArgs mevent) {
			base.OnMouseUp(mevent);
			if (ClientRectangle.Contains(mevent.Location)) {
				animationTarget = AnimationTarget.Hover;
				animationProgress = 1;
			}
			else {
				animationTarget = AnimationTarget.Idle;
				animationProgress = 0;
			}
			Invalidate();
		}

		protected override void OnPaint(PaintEventArgs e) {
			e.Graphics.Clear(Parent.BackColor);

			Color fore, back;
			if (!Enabled) {
				fore = DisabledForeColor;
				back = DisabledBackColor;
			}
			else if (animationTarget == AnimationTarget.MouseDown) {
				fore = DownForeColor;
				back = DownBackColor;
			}
			else {
				fore = ColorUtils.MixColors(HoverForeColor, IdleForeColor, Ease(animationProgress));
				back = ColorUtils.MixColors(HoverBackColor, IdleBackColor, Ease(animationProgress));
			}

			using (Brush brush = new SolidBrush(back))
				e.Graphics.FillRectangle(brush, 0, 0, Width, Height);

			SizeF size = e.Graphics.MeasureString(Text, Font);
			using (Brush brush = new SolidBrush(fore)) {
				e.Graphics.DrawString(Text, Font, brush, (Width - size.Width) / 2, (Height - size.Height) / 2);

				if (Glyph != null && Glyph.Path != null && Glyph.Size > 0) {
					PointF[] points = new PointF[Glyph.Path.Length];

					int minSide = Math.Min(Size.Width, Size.Height);
					int iconWidth = minSide - Padding.Right - Padding.Left;
					int iconHeight = minSide - Padding.Top - Padding.Bottom;

					for (int i = 0; i < points.Length; i++) {
						points[i].X = Glyph.Path[i].X * iconWidth / Glyph.Size + (Size.Width - iconWidth) / 2;
						points[i].Y = Glyph.Path[i].Y * iconHeight / Glyph.Size + (Size.Height - iconHeight) / 2;
					}

					e.Graphics.FillPolygon(brush, points);
				}
			}

			RaisePaintEvent(this, e);
		}

		protected override void Dispose(bool disposing) => base.Dispose(disposing);

		protected static float Ease(float value) => value * value;

		protected enum AnimationTarget {
			Hover, Idle, MouseDown, None
		}
	}
}
