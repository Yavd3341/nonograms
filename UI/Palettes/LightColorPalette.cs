using System.Drawing;

namespace JapaneseCrossword.UI.Palettes {
	public struct LightColorPalette : IColorPalette {

		//
		// Palette
		//

		public static readonly Color Default = ColorTranslator.FromHtml("#EAEAEA");

		public static readonly Color AccentLightLight = ColorTranslator.FromHtml("#40BEBEBE");
		public static readonly Color AccentLight = ColorTranslator.FromHtml("#80BEBEBE");
		public static readonly Color Accent = ColorTranslator.FromHtml("#A0A0A0");
		public static readonly Color AccentDark = ColorTranslator.FromHtml("#808080");
		public static readonly Color AccentDarkDark = ColorTranslator.FromHtml("#404040");

		public static readonly Color AlternativeRed = ColorTranslator.FromHtml("#BC4749");
		public static readonly Color AlternativeGold = ColorTranslator.FromHtml("#FFD700");

		//
		// Interface implementation
		//

		public Color FormForeColor => AccentDarkDark;
		public Color FormBackColor => Default;
		public Color FormHeaderColor => ColorUtils.ChangeOpacity(AccentLight, 0.3f);

		public Color ButtonIdleForeColor => AccentDarkDark;
		public Color ButtonIdleBackColor => ColorUtils.ChangeOpacity(AccentLight, 0.3f);
		public Color ButtonHoverForeColor => AccentDarkDark;
		public Color ButtonHoverBackColor => ColorUtils.ChangeOpacity(AccentLight, 0.8f);
		public Color ButtonDownForeColor => AccentDarkDark;
		public Color ButtonDownBackColor => ColorUtils.ChangeOpacity(AccentLight, 1.0f);
		public Color ButtonDisabledForeColor => Color.Gray;
		public Color ButtonDisabledBackColor => ColorUtils.ChangeOpacity(Color.Gray, 0.3f);

		public Color CloseButtonIdleForeColor => AlternativeRed;
		public Color CloseButtonIdleBackColor => ButtonIdleBackColor;
		public Color CloseButtonHoverForeColor => AlternativeRed;
		public Color CloseButtonHoverBackColor => ButtonHoverBackColor;
		public Color CloseButtonDownForeColor => AlternativeRed;
		public Color CloseButtonDownBackColor => ButtonDownBackColor;
		public Color CloseButtonDisabledForeColor => ButtonDisabledForeColor;
		public Color CloseButtonDisabledBackColor => ButtonDisabledBackColor;

		public Color MiscColors1 => AccentLightLight;
		public Color MiscColors2 => AccentDark;
		public Color MiscColors3 => AccentDark;
		public Color MiscColors4 => AlternativeGold;
		public Color MiscColors5 => Color.Transparent;
	}
}
