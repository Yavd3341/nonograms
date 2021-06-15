using JapaneseCrossword.UI.Controls;
using System.Drawing;
using System.Windows.Forms;

namespace JapaneseCrossword {
	public class SizeSelectionForm : JCWForm {

		public new int Width { get; private set; }
		public new int Height { get; private set; }

		public SizeSelectionForm() {

			//
			// Width controls
			//

			NumericUpDown widthNUD = new NumericUpDown() {
				DecimalPlaces = 0,
				Minimum = 10,
				Maximum = 256,
				Width = 75,
				Location = new Point(110, 25)
			};
			Controls.Add(widthNUD);

			Label label = new Label() {
				Text = "Ширина",
				TextAlign = ContentAlignment.TopRight,
				Location = new Point(widthNUD.Left - 85, widthNUD.Top),
				Size = new Size(75, widthNUD.Height)
			};
			Controls.Add(label);

			//
			// Height controls
			//

			NumericUpDown heightNUD = new NumericUpDown() {
				DecimalPlaces = widthNUD.DecimalPlaces,
				Minimum = widthNUD.Minimum,
				Maximum = widthNUD.Maximum,
				Width = widthNUD.Width,
				Location = new Point(widthNUD.Left, widthNUD.Bottom + 10)
			};
			Controls.Add(heightNUD);

			label = new Label() {
				Text = "Висота",
				TextAlign = ContentAlignment.TopRight,
				Location = new Point(heightNUD.Left - 85, heightNUD.Top),
				Size = label.Size
			};
			Controls.Add(label);

			//
			// Buttons
			//

			JCWButton button = new JCWButton(this) {
				Text = "Ок",
				Size = new Size(75, 35),
				Location = new Point(25, heightNUD.Bottom + 15),
				DialogResult = DialogResult.OK
			};
			button.Click += (sender, e) => {
				Width = (int) widthNUD.Value;
				Height = (int) heightNUD.Value;
			};
			AcceptButton = button;
			Controls.Add(button);

			button = new JCWButton(this) {
				Text = "Скасувати",
				Size = button.Size,
				Location = new Point(button.Right + 10, button.Top),
				DialogResult = DialogResult.Cancel
			};
			CancelButton = button;
			Controls.Add(button);

			//
			// Form 
			//

			Text = "Вибір розміру";
			MaximizeBox = false;
			CanResize = ResizableDimensions.None;
			ClientSize = new Size(button.Right + 25, button.Bottom + 25);
			Palette = Program.Palette;
		}

		public static Size GetNewImageSize() {
			SizeSelectionForm ssf = new SizeSelectionForm();
			return ssf.ShowDialog() == DialogResult.OK ? new Size(ssf.Width, ssf.Height) : Size.Empty;
		}
	}
}
