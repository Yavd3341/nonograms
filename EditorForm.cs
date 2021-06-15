using JapaneseCrossword.UI;
using JapaneseCrossword.UI.Controls;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace JapaneseCrossword {
	public class EditorForm : JCWForm {
		private bool changed = false;

		private readonly EditorPane editorPane;
		private readonly PaletteControl paletteControl;
		private readonly JCWChainedButton saveBtn;
		private readonly SaveFileDialog sfd;

		private Color[] internalPalette;

		public EditorForm(Size size) {
			//
			// Form and animation timer
			//

			Font = new Font(UIUtils.Bahnschrift, 16, FontStyle.Bold, GraphicsUnit.Pixel);
			Text = "Японський кросворд - Редактор";
			FormClosing += (sender, e) => e.Cancel = !SafeExit();
			FormClosed += (sender, e) => Program.MenuForm.Close();
			internalPalette = new Color[1];
			internalPalette[0] = Color.White;

			//
			// Button-creating function
			//

			JCWChainedButton GetButton(string text, JCWChainedButton prev, JCWChainedButton.ChainDirection cd) => new JCWChainedButton(this) {
				Text = text,
				Location = new Point(25, 25),
				Size = new Size(125, 32),
				Margin = new Padding(8),
				Padding = new Padding(6),
				ChainedControl = prev,
				ChainingDirection = cd
			};

			//
			// Top menu: Create, Save, Optimize and Add Color
			//

			JCWChainedButton newBtn = GetButton("Створити", null, JCWChainedButton.ChainDirection.Down);
			newBtn.Click += (sender, e) => {
				if (SafeExit()) {
					Size newImageSize = SizeSelectionForm.GetNewImageSize();
					if (newImageSize != Size.Empty)
						editorPane.NewImage(newImageSize);
				}
			};
			Controls.Add(newBtn);

			sfd = new SaveFileDialog() {
				AddExtension = true,
				DefaultExt = "jcw",
				Filter = "Файли кросворду|*.bin;*.jcw|Усі файли|*.*",
				Title = "Зберегти кросворд",
				InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)
			};

			ColorDialog colorDialog = new ColorDialog() {
				SolidColorOnly = true,
				FullOpen = true
			};

			saveBtn = GetButton("Зберегти", newBtn, JCWChainedButton.ChainDirection.Down);
			saveBtn.Click += (sender, e) => Save();
			Controls.Add(saveBtn);

			JCWChainedButton optimizeColorsBtn = GetButton("Оптимізувати", saveBtn, JCWChainedButton.ChainDirection.Down);
			optimizeColorsBtn.Click += (sedner, e) => {
				if (MessageBox.Show(
					"Оптимізувати кольори?\nЦя дія незворотня.",
					"Ви впевнені?",
					MessageBoxButtons.YesNo,
					MessageBoxIcon.Warning,
					MessageBoxDefaultButton.Button1
				) == DialogResult.Yes)
					Optimize();
			};
			Controls.Add(optimizeColorsBtn);

			JCWChainedButton addColorBtn = GetButton("Додати колір", optimizeColorsBtn, JCWChainedButton.ChainDirection.Down);
			addColorBtn.Click += (sender, e) => {
				if (colorDialog.ShowDialog() == DialogResult.OK)
					TryAddColor(colorDialog.Color);
			};
			Controls.Add(addColorBtn);

			addColorBtn.RecalculatePosition(true, true);

			//
			// Editor field
			//

			editorPane = new EditorPane() {
				Location = new Point(addColorBtn.Right + 25, 25),
				CellSide = 25,
				Font = new Font(UIUtils.Bahnschrift, 12, FontStyle.Bold, GraphicsUnit.Pixel),
				Visible = true
			};
			editorPane.NewImage(size);
			Width = editorPane.Right + 25;
			editorPane.DataChanged += (sender, e) => changed = true;
			Controls.Add(editorPane);

			//
			// Palette
			//

			Label label = new Label() {
				Location = new Point(25, addColorBtn.Bottom + 25),
				AutoSize = true,
				Text = "Палитра"
			};
			Controls.Add(label);

			paletteControl = new PaletteControl(this) {
				Location = new Point(25, label.Bottom + 5),
				CellSide = 25,
				SkipFirst = false
			};
			paletteControl.ColorChange += (sender, e) => editorPane.SelectedColor = paletteControl.SelectedColor;
			paletteControl.UpdateSize();
			Controls.Add(paletteControl);

			UpdatePalettes(internalPalette);

			//
			// Finish init
			//

			ClientSize = new Size(
				Math.Min(editorPane.Right + 25, 750),
				paletteControl.Bottom + 25 + (editorPane.Right + 25 > 750 ? SystemInformation.HorizontalScrollBarHeight : 0)
			);

			Palette = Program.Palette;
		}

		public bool Save() {
			if (sfd.ShowDialog() == DialogResult.OK) {
				Optimize();

				Crossword cw = new Crossword() {
					Field = editorPane.Field,
					Palette = internalPalette
				};

				bool full = false;

				if (internalPalette.Length == 2)
					full = MessageBox.Show(
						"Зберегти кросворд як чорно-білий?",
						"Збереження",
						MessageBoxButtons.YesNo,
						MessageBoxIcon.Exclamation,
						MessageBoxDefaultButton.Button1
					) == DialogResult.No;

				cw.OptimizeField();
				try {
					cw.Save(sfd.FileName, full ? Crossword.FileFormat.ColorCompressed : Crossword.FileFormat.Default);
				}
				catch (InvalidOperationException) {
					MessageBox.Show(
						"Неможливо зберегти кросворд: поле порожнє.",
						"Помилка при збереженні",
						MessageBoxButtons.OK,
						MessageBoxIcon.Error,
						MessageBoxDefaultButton.Button1
					);
				}

				changed = false;
				return true;
			}
			return false;
		}

		public void Optimize() {
			Crossword cw = new Crossword() {
				Palette = internalPalette,
				Field = editorPane.Field
			};
			cw.OptimizePalette();
			internalPalette = cw.Palette;
			UpdatePalettes(internalPalette);
		}

		public void TryAddColor(Color color) {
			if (internalPalette.Length == 256) {
				if (MessageBox.Show(
					"Неможливо додати колір: вичерпано місце у палітрі. Спробуйте оптимізувати її.\nСпробувати зробити це зараз?",
					"Помилка при додаванні",
					MessageBoxButtons.YesNo,
					MessageBoxIcon.Error,
					MessageBoxDefaultButton.Button2
				) == DialogResult.Yes) {
					Optimize();
					if (internalPalette.Length == 256) {
						MessageBox.Show(
							"Неможливо додати колір: вичерпано місце у палітрі.",
							"Помилка при додаванні",
							MessageBoxButtons.OK,
							MessageBoxIcon.Error,
							MessageBoxDefaultButton.Button1
						);
						return;
					}
				}
				else
					return;
			}

			Color[] newPalette = new Color[internalPalette.Length + 1];
			Array.Copy(internalPalette, newPalette, internalPalette.Length);
			newPalette[newPalette.Length - 1] = color;
			UpdatePalettes(newPalette);
			paletteControl.ChangePage((newPalette.Length - 1) / (paletteControl.RowCount * paletteControl.ColumnCount));
			int cellNo = (newPalette.Length - 1) % (paletteControl.RowCount * paletteControl.ColumnCount);
			paletteControl.SelectCell(cellNo % paletteControl.ColumnCount, cellNo / paletteControl.ColumnCount);
		}

		private void UpdatePalettes(Color[] palette) {
			internalPalette = palette;
			paletteControl.ColorPalette = internalPalette;
			editorPane.ColorPalette = internalPalette;
			saveBtn.Enabled = internalPalette.Length > 1;
		}

		public bool SafeExit() {
			if (changed) {
				DialogResult safeDialog = MessageBox.Show(
					"Ваш кросворд не збережений! Ваш прогрес буде втрачено!\nЗберегти його?",
					"Ви впевнені?",
					MessageBoxButtons.YesNoCancel,
					MessageBoxIcon.Warning,
					MessageBoxDefaultButton.Button1
				);

				return safeDialog == DialogResult.Yes ? Save() : safeDialog == DialogResult.No;
			}
			return true;
		}
	}
}
