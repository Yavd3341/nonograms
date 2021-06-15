using System.Drawing;

namespace JapaneseCrossword.UI.Palettes {
	public interface IColorPalette {
		Color FormForeColor { get; }
		Color FormBackColor { get; }
		Color FormHeaderColor { get; }

		Color ButtonIdleForeColor { get; }
		Color ButtonIdleBackColor { get; }
		Color ButtonHoverForeColor { get; }
		Color ButtonHoverBackColor { get; }
		Color ButtonDownForeColor { get; }
		Color ButtonDownBackColor { get; }
		Color ButtonDisabledForeColor { get; }
		Color ButtonDisabledBackColor { get; }

		Color CloseButtonIdleForeColor { get; }
		Color CloseButtonIdleBackColor { get; }
		Color CloseButtonHoverForeColor { get; }
		Color CloseButtonHoverBackColor { get; }
		Color CloseButtonDownForeColor { get; }
		Color CloseButtonDownBackColor { get; }
		Color CloseButtonDisabledForeColor { get; }
		Color CloseButtonDisabledBackColor { get; }

		Color MiscColors1 { get; }
		Color MiscColors2 { get; }
		Color MiscColors3 { get; }
		Color MiscColors4 { get; }
		Color MiscColors5 { get; }
	}
}
