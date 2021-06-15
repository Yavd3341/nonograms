using System;
using System.Drawing;
using System.Windows.Forms;

namespace JapaneseCrossword.UI.Controls {
	public class JCWChainedButton : JCWButton {
		private Control chainedControl;

		public JCWChainedButton(IAnimator animator) : base(animator) { }

		protected JCWChainedButton NextChainedControl { get; set; }
		public Control ChainedControl {
			get => chainedControl;
			set {
				if (chainedControl is JCWChainedButton chainedButtonOld)
					chainedButtonOld.NextChainedControl = null;

				chainedControl = value;

				if (chainedControl is JCWChainedButton chainedButtonNew)
					chainedButtonNew.NextChainedControl = this;

				RecalculatePosition(false, true);
			}
		}

		public ChainDirection ChainingDirection { get; set; }

		public void RecalculatePosition(bool updatePrev, bool updateNext) {
			if (ChainedControl != null && ChainingDirection != ChainDirection.None) {

				if (updatePrev && ChainedControl is JCWChainedButton chainedButton)
					chainedButton.RecalculatePosition(updatePrev, false);

				int x = ChainedControl.Location.X, y = ChainedControl.Location.Y;

				if (ChainingDirection.HasFlag(ChainDirection.Right))
					x = ChainedControl.Right + Margin.Left;
				else if (ChainingDirection.HasFlag(ChainDirection.Left))
					x = ChainedControl.Left - Margin.Right - Width;

				if (ChainingDirection.HasFlag(ChainDirection.Down))
					y = ChainedControl.Bottom + Margin.Top;
				else if (ChainingDirection.HasFlag(ChainDirection.Up))
					y = ChainedControl.Top - Margin.Bottom - Height;

				Location = new Point(x, y);
				Anchor = chainedControl.Anchor;
			}

			if (updateNext && NextChainedControl != null)
				NextChainedControl.RecalculatePosition(false, updateNext);
		}

		[Flags]
		public enum ChainDirection {
			None = 0,
			Down = 1,
			Up = 2,
			Right = 4,
			Left = 8
		}
	}
}
