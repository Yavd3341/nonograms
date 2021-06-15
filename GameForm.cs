using JapaneseCrossword.UI;
using JapaneseCrossword.UI.Controls;
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JapaneseCrossword {
	public class GameForm : JCWForm {
		private bool running = true, started = false;

		private readonly GameField gameField;
		private readonly PaletteControl palette;
		private readonly Timer timer;
		private readonly JCWChainedButton undoBtn, restartBtn, checkBtn, solutionBtn;
		private readonly Label timeLabel, stepsLabel;

		private uint wrongCells;

		private ushort seconds;

		public bool Valid { get; protected set; }

		private Crossword crossword;
		public Crossword Crossword {
			get => crossword;
			set {
				crossword = value;
				wrongCells = (uint) (crossword.Width * crossword.Height);

				for (int x = 0; x < crossword.Width; x++)
					for (int y = 0; y < crossword.Height; y++)
						if (crossword.Field[x, y] == 0x00)
							wrongCells--;

				gameField.Crossword = crossword;
				gameField.Visible = true;
				gameField.RebuildCache();

				ClientSize = new Size(
					Math.Min(gameField.Right + 25, 750),
					solutionBtn.Bottom + 25 + (gameField.Right + 25 > 750 ? SystemInformation.HorizontalScrollBarHeight : 0)
				);

				palette.ColorPalette = crossword.Palette;

				timer.Stop();
				seconds = 0;

				timeLabel.Text = "0:00:00";
				stepsLabel.Text = "0";

				undoBtn.Enabled = false;
				restartBtn.Enabled = false;
				checkBtn.Enabled = false;
				solutionBtn.Enabled = true;

				running = wrongCells > 0;
				started = false;
				Valid = true;
			}
		}

		public GameForm() {

			//
			// Form and game timer init
			//

			Font = new Font(UIUtils.Bahnschrift, 16, FontStyle.Bold, GraphicsUnit.Pixel);
			Text = "Японський кросворд";
			FormClosing += (sender, e) => e.Cancel = !SafeExit();
			FormClosed += (sender, e) => Program.MenuForm.Close();

			VisibleChanged += (sender, e) => {
				if (running && started)
					timer.Start();
			};

			timer = new Timer() {
				Interval = 1000,
				Enabled = false
			};

			timer.Tick += (sender, e) => {
				seconds++;
				timeLabel.Text = string.Format($"{seconds / 60 / 60 % 60}:{seconds / 60 % 60:D02}:{seconds % 60:D02}");

				if (seconds == ushort.MaxValue) {
					StopGame();
					MessageBox.Show("Час вичерпано", "Програш", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
				}
			};

			//
			// Button-creating function
			//

			JCWChainedButton GetButton(string text, JCWChainedButton prev, JCWChainedButton.ChainDirection cd) => new JCWChainedButton(this) {
				Text = text,
				Location = new Point(25, 25),
				Size = new Size(125, 32),
				Margin = new Padding(8),
				ChainedControl = prev,
				ChainingDirection = cd,
			};

			//
			// Top menu: Menu, Undo, Restart, Check
			//

			JCWChainedButton button = GetButton("Меню", null, JCWChainedButton.ChainDirection.None);
			button.Click += (sender, args) => {
				Program.MenuForm.Location = Location;
				Program.MenuForm.Show();
				Hide();
				timer.Stop();
			};
			Controls.Add(button);

			undoBtn = GetButton("", button, JCWChainedButton.ChainDirection.Down);
			undoBtn.Padding = new Padding(6);
			undoBtn.Glyph = new Glyph(new Point[]  {
					new Point(0, 4),
					new Point(4,0),
					new Point(4,3),
					new Point(8,3),
					new Point(8,5),
					new Point(4,5),
					new Point(4,8)
			}, 8);
			undoBtn.Enabled = false;
			Controls.Add(undoBtn);

			restartBtn = GetButton("Заново", undoBtn, JCWChainedButton.ChainDirection.Down);
			restartBtn.Click += (sender, e) => {
				if (!(started && running) || MessageBox.Show(
					"Ви дійсно бажаєте розпочати наново? Повернутись до цієї гри буде неможливо!",
					"Чи ві впевнені?",
					MessageBoxButtons.YesNo,
					MessageBoxIcon.Warning,
					MessageBoxDefaultButton.Button2
				) == DialogResult.Yes)
					Crossword = Crossword;
			};
			restartBtn.Enabled = false;
			Controls.Add(restartBtn);

			checkBtn = GetButton("Перевірити", restartBtn, JCWChainedButton.ChainDirection.Down);
			checkBtn.Click += (sender, e) => CheckSolution(true, true);
			restartBtn.Enabled = false;
			Controls.Add(checkBtn);

			checkBtn.RecalculatePosition(true, true);

			//
			// Game field
			//

			gameField = new GameField() {
				Location = new Point(checkBtn.Right + 25, 25),
				CellSide = 25,
				Font = new Font(UIUtils.Bahnschrift, 12, FontStyle.Bold, GraphicsUnit.Pixel),
				Visible = true
			};

			gameField.CellUpdate += (sender, e) => {
				if (running) {
					if (e.NewColor == crossword.Field[e.X, e.Y])
						wrongCells--;
					else if (e.Color == crossword.Field[e.X, e.Y])
						wrongCells++;

					CheckSolution(false, false);

					if (!started) {
						timer.Start();
						started = true;
						checkBtn.Enabled = true;
						restartBtn.Enabled = true;
					}
				}
				else
					e.Cancel = true;
			};

			gameField.LastActionsUpdate += (sender, e) => {
				undoBtn.Enabled = gameField.ActionsDone > 0;
				stepsLabel.Text = gameField.ActionsDone.ToString();
			};

			undoBtn.Click += (sender, e) => gameField.Undo();
			Controls.Add(gameField);

			//
			// Labels
			//

			Label label = new Label() {
				Location = new Point(25, checkBtn.Bottom + 25),
				TextAlign = ContentAlignment.MiddleRight,
				AutoSize = true,
				Text = "Час:"
			};
			Controls.Add(label);

			timeLabel = new Label() {
				Location = new Point(label.Right + 5, label.Top),
				TextAlign = ContentAlignment.MiddleRight,
				Size = new Size(button.Right - label.Right, label.Height),
				Text = "0:00:00"
			};
			Controls.Add(timeLabel);

			label = new Label() {
				Location = new Point(25, label.Bottom + 5),
				TextAlign = ContentAlignment.MiddleRight,
				AutoSize = true,
				Text = "Кроків:"
			};
			Controls.Add(label);

			stepsLabel = new Label() {
				Location = new Point(label.Right + 5, label.Top),
				TextAlign = ContentAlignment.MiddleRight,
				Size = new Size(button.Right - label.Right, label.Height),
				Text = "0"
			};
			Controls.Add(stepsLabel);

			//
			// Palette
			//

			label = new Label() {
				Location = new Point(25, label.Bottom + 25),
				AutoSize = true,
				Text = "Палитра"
			};
			Controls.Add(label);

			palette = new PaletteControl(this) {
				Location = new Point(25, label.Bottom + 5),
				CellSide = 25,
			};
			palette.ColorChange += (sender, e) => gameField.SelectedColor = palette.SelectedColor;
			palette.UpdateSize();
			Controls.Add(palette);

			//
			// "Show solution" button
			//

			solutionBtn = GetButton("Відповідь", null, JCWChainedButton.ChainDirection.None);
			solutionBtn.Location = new Point(25, palette.Bottom + 25);
			solutionBtn.Click += (sender, args) => {
				gameField.ShowSolution();
				StopGame();
			};
			solutionBtn.Enabled = false;
			Controls.Add(solutionBtn);

			Palette = Program.Palette;
		}

		public void CheckSolution(bool announceWrong, bool deepCheck) => Task.Run(() => {
			bool right = true;

			if (deepCheck)
				for (int x = 0; x < crossword.Width && right; x++)
					for (int y = 0; y < crossword.Height && right; y++)
						right = right && crossword.Field[x, y] == gameField[x, y];
			else
				right = wrongCells == 0;

			if (right) {
				Invoke(new Action(StopGame));
				MessageBox.Show("Рішення знайдено", "Вітаємо!", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
			}
			else if (announceWrong)
				MessageBox.Show("Рішення ще не знайдено", "Ще ні", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
		});

		public void StopGame() {
			timer.Stop();
			running = false;
			undoBtn.Enabled = false;
			checkBtn.Enabled = false;
			solutionBtn.Enabled = false;
			restartBtn.Enabled = true;
		}

		public bool SafeExit() {
			if (!(running && started) || MessageBox.Show(
				"Ви впевнені що хочете вийти? Ваш прогрес буде втрачено!",
				"Ви впевнені?",
				MessageBoxButtons.YesNo,
				MessageBoxIcon.Warning,
				MessageBoxDefaultButton.Button2
			) == DialogResult.Yes) {
				running = false;
				started = false;
				timer.Stop();
				return true;
			}
			else
				return false;
		}
	}
}
