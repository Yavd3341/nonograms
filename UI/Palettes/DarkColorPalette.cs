using System.Drawing;

namespace JapaneseCrossword.UI.Palettes {
	public struct DarkColorPalette : IColorPalette {

		//
		// Palette
		//

		public static readonly Color Default = ColorTranslator.FromHtml("#202020");

		public static readonly Color AccentLightLight = ColorTranslator.FromHtml("#E0E0E0");
		public static readonly Color AccentLightTransparent = ColorTranslator.FromHtml("#40A0A0A0");
		public static readonly Color AccentLight = ColorTranslator.FromHtml("#A0A0A0");
		public static readonly Color Accent = ColorTranslator.FromHtml("#404040");
		public static readonly Color AccentDark = ColorTranslator.FromHtml("#101010");
		public static readonly Color AccentDarkDark = ColorTranslator.FromHtml("#000000");

		public static readonly Color AlternativeRed = ColorTranslator.FromHtml("#BC4749");
		public static readonly Color AlternativeGold = ColorTranslator.FromHtml("#FFD700");

		//
		// Interface implementation
		//

		public Color FormForeColor => AccentLight;
		public Color FormBackColor => Default;
		public Color FormHeaderColor => ColorUtils.ChangeOpacity(AccentDark, 0.3f);

		public Color ButtonIdleForeColor => AccentLight;
		public Color ButtonIdleBackColor => ColorUtils.ChangeOpacity(AccentDark, 0.3f);
		public Color ButtonHoverForeColor => AccentLightLight;
		public Color ButtonHoverBackColor => ColorUtils.ChangeOpacity(AccentDark, 0.8f);
		public Color ButtonDownForeColor => AccentLightLight;
		public Color ButtonDownBackColor => ColorUtils.ChangeOpacity(AccentDark, 1.0f);
		public Color ButtonDisabledForeColor => AccentLight;
		public Color ButtonDisabledBackColor => ColorUtils.ChangeOpacity(Accent, 0.3f);

		public Color CloseButtonIdleForeColor => AlternativeRed;
		public Color CloseButtonIdleBackColor => ButtonIdleBackColor;
		public Color CloseButtonHoverForeColor => AlternativeRed;
		public Color CloseButtonHoverBackColor => ButtonHoverBackColor;
		public Color CloseButtonDownForeColor => AlternativeRed;
		public Color CloseButtonDownBackColor => ButtonDownBackColor;
		public Color CloseButtonDisabledForeColor => ButtonDisabledForeColor;
		public Color CloseButtonDisabledBackColor => ButtonDisabledBackColor;

		public Color MiscColors1 => AccentLightTransparent;
		public Color MiscColors2 => AccentDark;
		public Color MiscColors3 => AccentDark;
		public Color MiscColors4 => AlternativeGold;
		public Color MiscColors5 => Color.Transparent;
	}
}
