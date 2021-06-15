using System;
using System.Drawing;
using System.IO;

namespace JapaneseCrossword {
	public struct Crossword : ICloneable {
		public Color[] Palette { get; set; }
		public byte[,] Field { get; set; }

		public int Height => Field.GetLength(1);
		public int Width => Field.GetLength(0);

		//
		// File handling
		//

		// File header ("JCW")
		private static readonly byte[] FILE_SIGNATURE = { 0x4A, 0x43, 0x57 };

		// Control byte (flags byte) masks
		private const byte FLAG_MONOCHROME = 0b10000000;
		private const byte MASK_PALETTE = 0b01111111;

		// Data byte (monochrome) masks
		private const byte DATM_COLOR = 0b10000000;
		private const byte DATM_LENGTH = 0b01111111;

		// Data byte (color) masks - CALCULATED AT FILE LOAD
		private static byte DATC_COLOR;
		private static byte DATC_EXTENDED;
		private static byte DATC_LENGTH;
		private static byte DATC_OFFSET;

		public enum FileFormat {
			Default, ColorCompressed, ColorFull
		}

		public static Crossword Load(Stream stream) {

			// Check file signature
			for (int i = 0; i < FILE_SIGNATURE.Length; i++)
				if (stream.ReadByte() != FILE_SIGNATURE[i])
					throw new InvalidDataException("Stream doesn't contain crossword data");

			// Load flags
			byte flags = (byte) stream.ReadByte();
			bool monochrome = (flags & FLAG_MONOCHROME) > 0;
			bool fullFormat = (flags & MASK_PALETTE) == 0;

			// Prepare container
			int wlength = stream.ReadByte() + 1;
			int hlength = stream.ReadByte() + 1;
			Crossword jcw = new Crossword() { Field = new byte[wlength, hlength] };

			// Load palette
			if (!monochrome) {
				short paletteLength = (short) ((fullFormat ? stream.ReadByte() : flags & MASK_PALETTE) + 1);
				jcw.Palette = new Color[paletteLength == 257 ? 256 : paletteLength];

				// Populate palette
				for (int i = 1; i < jcw.Palette.Length; i++)
					jcw.Palette[i] = Color.FromArgb(stream.ReadByte(), stream.ReadByte(), stream.ReadByte());

				// Skip 257th color
				if (paletteLength == 257) {
					stream.ReadByte();
					stream.ReadByte();
					stream.ReadByte();
				}

				jcw.Palette[0] = Color.White;

				// Calculate data masks
				if (!fullFormat) {
					int tmp = 1, offset = 8;
					DATC_COLOR = 0;
					for (int i = 0; i < 6 && jcw.Palette.Length > tmp; i++, tmp *= 2, offset--)
						DATC_COLOR = (byte) ((DATC_COLOR >> 1) | 0b10000000);
					DATC_EXTENDED = (byte) (DATC_COLOR ^ ((DATC_COLOR >> 1) | 0b10000000));
					DATC_LENGTH = (byte) ~(0xFF & (DATC_COLOR | DATC_EXTENDED));
					DATC_OFFSET = (byte) (offset - 1);
				}
			}
			else
				jcw.Palette = new Color[] { Color.White, Color.Black };

			// Load field
			int pixel = 0, length = 0;
			byte color = 0;
			while (pixel < wlength * hlength) {
				// Color select
				if (length == 0) {
					try {
						if (monochrome) {
							byte data = (byte) stream.ReadByte();
							color = (byte) ((data & DATM_COLOR) == DATM_COLOR ? 1 : 0);
							length = (data & DATM_LENGTH) + 1;
						}
						else {
							if (fullFormat) {
								color = (byte) stream.ReadByte();
								length = stream.ReadByte() + 1;
							}
							else {
								byte data = (byte) stream.ReadByte();
								color = (byte) ((data & DATC_COLOR) >> (DATC_OFFSET + 1));
								length = (data & DATC_LENGTH) + 1;
								if ((data & DATC_EXTENDED) > 0)
									length += stream.ReadByte() + 1;
							}
						}

						if (color >= jcw.Palette.Length)
							color = 0;
					}
					catch (Exception e) {
						Console.Error.WriteLine(e);
						length = wlength * hlength - pixel;
						color = 0;
					}
				}

				// Apply color for pixel
				jcw.Field[pixel % wlength, pixel / wlength] = color;
				pixel++;
				length--;
			}

			return jcw;
		}
		public static Crossword Load(string path) {
			using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
				return Load(fs);
		}

		public void Save(Stream stream, FileFormat fileFormat = FileFormat.Default) {
			// Size sanity check
			if (Height == 0 || Width == 0)
				throw new InvalidOperationException("Can't save empty crossword.");

			// Write header
			stream.Write(FILE_SIGNATURE, 0, FILE_SIGNATURE.Length);

			if (Palette.Length > 2 || fileFormat > FileFormat.Default) {

				if (Palette.Length > 256)
					throw new ArgumentOutOfRangeException("Palette too big to be stored. You can try to optimize it.");

				if (Palette.Length > MASK_PALETTE || fileFormat == FileFormat.ColorFull) {
					// Write flags and size
					stream.WriteByte(0);
					stream.WriteByte((byte) (Height - 1));
					stream.WriteByte((byte) (Width - 1));

					// Write palette
					stream.WriteByte((byte) (Palette.Length - 1));
					for (int i = 1; i < Palette.Length; i++) {
						Color color = Palette[i];
						stream.WriteByte(color.R);
						stream.WriteByte(color.G);
						stream.WriteByte(color.B);
					}

					// Write data
					byte colorIndex = Field[0, 0];
					short count = 0;
					for (short y = 0; y < Height; y++) {
						for (short x = 0; x < Width; x++, count++) {
							if (colorIndex != Field[x, y] || count > 255) {
								stream.WriteByte(colorIndex);
								stream.WriteByte((byte) (count - 1));

								colorIndex = Field[x, y];
								count = 0;
							}
						}
					}

					// Write last region
					stream.WriteByte(colorIndex);
					stream.WriteByte((byte) (count - 1));
				}
				else {
					// Calculate data masks
					int tmp = 1, offset = 8;
					DATC_COLOR = 0;
					for (int i = 0; i < 6 && Palette.Length > tmp; i++, tmp *= 2, offset--)
						DATC_COLOR = (byte) ((DATC_COLOR >> 1) | 0b10000000);
					DATC_EXTENDED = (byte) (DATC_COLOR ^ ((DATC_COLOR >> 1) | 0b10000000));
					DATC_LENGTH = (byte) ~(0xFF & (DATC_COLOR | DATC_EXTENDED));
					DATC_OFFSET = (byte) (offset - 1);

					// Write flags and size
					stream.WriteByte((byte) ((Palette.Length - 1) & MASK_PALETTE));
					stream.WriteByte((byte) (Height - 1));
					stream.WriteByte((byte) (Width - 1));

					// Write palette
					for (int i = 1; i < Palette.Length; i++) {
						Color color = Palette[i];
						stream.WriteByte(color.R);
						stream.WriteByte(color.G);
						stream.WriteByte(color.B);
					}

					// Write data
					byte colorIndex = Field[0, 0];
					short count = 0;
					for (short y = 0; y < Height; y++) {
						for (short x = 0; x < Width; x++, count++) {
							if (colorIndex != Field[x, y] || count > DATC_LENGTH + 257) {
								stream.WriteByte((byte) (((colorIndex << (DATC_OFFSET + 1)) & DATC_COLOR)
									| (count - 1 > DATC_LENGTH ? DATC_EXTENDED : 0)
									| (count - 1 > DATC_LENGTH ? DATC_LENGTH : (count - 1) & DATC_LENGTH)));

								if (count - 1 > DATC_LENGTH)
									stream.WriteByte((byte) (count - 2 - DATC_LENGTH));

								colorIndex = Field[x, y];
								count = 0;
							}
						}
					}

					// Write last region
					stream.WriteByte((byte) (((colorIndex << (DATC_OFFSET + 1)) & DATC_COLOR)
						| (count - 1 > DATC_LENGTH ? DATC_EXTENDED : 0)
						| (count - 1 > DATC_LENGTH ? DATC_LENGTH : (count - 1) & DATC_LENGTH)));

					if (count - 1 > DATC_LENGTH)
						stream.WriteByte((byte) (count - 2 - DATC_LENGTH));

				}
			}
			else {
				// Write flags and size
				stream.WriteByte(FLAG_MONOCHROME);
				stream.WriteByte((byte) (Height - 1));
				stream.WriteByte((byte) (Width - 1));

				// Write data
				bool black = Field[0, 0] == 1;
				short count = 0;
				for (short y = 0; y < Height; y++) {
					for (short x = 0; x < Width; x++, count++) {
						if (black != (Field[x, y] == 1) || count > DATM_LENGTH) {
							stream.WriteByte((byte) (((count - 1) & DATM_LENGTH) | (black ? DATM_COLOR : 0)));

							black = Field[x, y] == 1;
							count = 0;
						}
					}
				}

				// Write last region
				stream.WriteByte((byte) (((count - 1) & DATM_LENGTH) | (black ? DATM_COLOR : 0)));
			}
		}
		public void Save(string path, FileFormat fileFormat = FileFormat.Default) {
			using (FileStream fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
				Save(fs, fileFormat);
		}

		public void OptimizePalette() {
			// Prepare map
			short[] paletteMap = new short[Palette.Length];

			// Calculate color usage
			for (int y = 0; y < Height; y++)
				for (int x = 0; x < Width; x++)
					paletteMap[Field[x, y]]++;

			// Preseve white color even if not existing
			// due to its "reserved" status
			paletteMap[0] = 1;

			// Make mapping
			byte counter = 0;
			for (int i = 0; i < paletteMap.Length; i++)
				paletteMap[i] = counter >= paletteMap.Length || paletteMap[i] == 0 ? (short) -1 : counter++;

			// Map field
			for (int x = 0; x < Width; x++)
				for (int y = 0; y < Height; y++)
					Field[x, y] = (byte) paletteMap[Field[x, y]];

			// Map palette
			Color[] newPalette = new Color[counter];
			for (int i = 0; i < Palette.Length; i++)
				if (paletteMap[i] != -1)
					newPalette[paletteMap[i]] = Palette[i];

			// Apply new palette
			Palette = newPalette;
		}

		public void OptimizeField() {
			int top, right, bottom, left;
			bool filled;
			byte[,] newField;

			// Find boundaries
			filled = false;
			for (top = 0; top < Height && !filled; top++)
				for (int x = 0; x < Width; x++)
					if (Field[x, top] != 0)
						filled = true;

			// End search if empty
			if (top == Height)
				newField = new byte[0, 0];
			else {
				top--;

				filled = false;
				for (bottom = Height - 1; bottom >= top && !filled; bottom--)
					for (int x = 0; x < Width; x++)
						if (Field[x, bottom] != 0)
							filled = true;
				bottom++;

				filled = false;
				for (right = 0; right < Width && !filled; right++)
					for (int y = 0; y < Height; y++)
						if (Field[right, y] != 0)
							filled = true;
				right--;

				filled = false;
				for (left = Width - 1; left >= right && !filled; left--)
					for (int y = 0; y < Height; y++)
						if (Field[left, y] != 0)
							filled = true;
				left++;

				// Build trimmed field
				newField = new byte[left - right + 1, bottom - top + 1];
				for (int y = top; y <= bottom; y++)
					for (int x = right; x <= left; x++)
						newField[x - right, y - top] = Field[x, y];
			}

			// Apply new field
			Field = newField;
		}

		public void Optimize() {
			OptimizeField();
			OptimizePalette();
		}

		public object Clone() {
			Crossword cw = new Crossword() {
				Palette = new Color[Palette.Length],
				Field = new byte[Width, Height]
			};

			for (int i = 0; i < Palette.Length; i++)
				cw.Palette[i] = Palette[i];

			for (int y = 0; y < Height; y++)
				for (int x = 0; x < Width; x++)
					cw.Field[x, y] = Field[x, y];

			return cw;
		}
	}
}
