using JapaneseCrossword.UI;
using JapaneseCrossword.UI.Controls;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace JapaneseCrossword {
	public class MenuForm : JCWForm {

		private readonly Font title, ver;

		public MenuForm() {
			Font = new Font(UIUtils.Bahnschrift, 16, FontStyle.Bold, GraphicsUnit.Pixel);
			title = new Font(UIUtils.Bahnschrift, 28, FontStyle.Bold, GraphicsUnit.Pixel);
			ver = new Font(UIUtils.Bahnschrift, 10, FontStyle.Regular, GraphicsUnit.Pixel);
			Text = "Японський кросворд";
			Size = new Size(400, 600);
			MaximizeBox = false;
			CanResize = ResizableDimensions.None;
			FormClosing += (sender, e) => e.Cancel = !Program.GameForm.SafeExit();
			FormClosed += (sender, e) => Application.Exit();
			UseDirectControls = true;
			content.Dispose();

			OpenFileDialog ofd = new OpenFileDialog() {
				Filter = "Файли кросворду|*.bin;*.jcw|Усі файли|*.*",
				Title = "Завантажити кросворд",
				InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)
			};

			JCWChainedButton GetMenuButton(string text, Point location, JCWChainedButton prev, JCWChainedButton.ChainDirection cd) => new JCWChainedButton(this) {
				Text = text,
				Location = location,
				Size = new Size(200, 48),
				Margin = new Padding(8),
				ChainedControl = prev,
				ChainingDirection = cd
			};

			JCWChainedButton newGameButton = GetMenuButton("Нова гра", new Point(100, 250), null, JCWChainedButton.ChainDirection.None);
			Controls.Add(newGameButton);

			JCWChainedButton resumeGameButton = GetMenuButton("Повернутись до гри", new Point(), newGameButton, JCWChainedButton.ChainDirection.Down);
			resumeGameButton.Enabled = false;
			resumeGameButton.Click += (sender, args) => {
				Program.GameForm.Location = Location;
				Program.GameForm.Show();
				Hide();
			};
			Controls.Add(resumeGameButton);

			JCWChainedButton openEditorButton = GetMenuButton("Відкрити редактор", new Point(), resumeGameButton, JCWChainedButton.ChainDirection.Down);
			openEditorButton.Click += (sender, args) => {
				Size size = SizeSelectionForm.GetNewImageSize();

				if (size != Size.Empty) {
					new EditorForm(size) {
						Location = Location
					}.Show();
					Hide();
				}
			};
			Controls.Add(openEditorButton);

			newGameButton.Click += (sender, args) => {
				if (ofd.ShowDialog() == DialogResult.OK) {
					while (true) {
						try {
							Program.GameForm.Crossword = Crossword.Load(ofd.FileName);
							openEditorButton.Enabled = !Program.GameForm.Valid;
							resumeGameButton.Enabled = Program.GameForm.Valid;
							resumeGameButton.PerformClick();
							break;
						}
						catch (Exception e) {
							Console.WriteLine(e);
							if (MessageBox.Show(
								"Кросворд не був завантажений через помилку.",
								"Помилка завантаження",
								MessageBoxButtons.RetryCancel,
								MessageBoxIcon.Error,
								MessageBoxDefaultButton.Button2
							) == DialogResult.Cancel)
								break;
						}
					}
				}
			};

			newGameButton.RecalculatePosition(true, true);

			Palette = Program.Palette;
		}

		protected override void OnPaint(PaintEventArgs e) {
			base.OnPaint(e);
			e.Graphics.Clear(BackColor);

			using (Brush brush = new SolidBrush(HeaderBackColor))
				e.Graphics.FillRectangle(brush, e.ClipRectangle);

			using (Brush brush = new SolidBrush(ForeColor)) {
				float y = 0;
				foreach (string token in Text.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)) {
					string tokenUp = token.ToUpper().Inflate(1);
					SizeF size = e.Graphics.MeasureString(tokenUp, title);
					e.Graphics.DrawString(tokenUp, title, brush, (Width - size.Width) / 2.0f, 100 + y);
					y += size.Height;
				}

				y = 0;
				string[] version = $"Ілля Явдощук - 2021\nВерсія: {Application.ProductVersion}".Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
				Array.Reverse(version);
				foreach (string token in version) {
					SizeF size = e.Graphics.MeasureString(token, ver);
					y += size.Height * 1.25f;
					e.Graphics.DrawString(token, ver, brush, (Width - size.Width) / 2.0f, Height - 25 - y);
				}
			}

			using (Pen pen = new Pen(ForeColor))
				e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
		}
	}
}
