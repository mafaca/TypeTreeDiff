using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace TypeTreeDiff
{
	public sealed class DumpReader : WordReader
	{
		public DumpReader(Stream stream) :
			base(stream, WordDelimiters.Union(DumpDelimiters))
		{
		}

		public string FindPeekLineWord()
		{
			bool isFound = FindLineContent();
			if (!isFound)
			{
				throw new Exception($"Can't find line word for reading");
			}

			return PeekWord();
		}

		public string FindReadLineWord()
		{
			bool isFound = FindLineContent();
			if (!isFound)
			{
				throw new Exception($"Can't find line word for reading");
			}

			return ReadWord();
		}

		public void ValidateWord(string expectedWord)
		{
			string word = ReadWord();
			if (word.ToLower(CultureInfo.InvariantCulture) != expectedWord.ToLower(CultureInfo.InvariantCulture))
			{
				throw new Exception($"Expected word '{expectedWord}' doesn't match to '{word}'");
			}
		}

		public void FindValidateWord(string expectedWord)
		{
			bool found = FindContent();
			if (!found)
			{
				throw new Exception($"Can't find content to validate word");
			}
			ValidateWord(expectedWord);
		}

		public void FindValidateLineWord(string expectedWord)
		{
			bool found = FindLineContent();
			if (!found)
			{
				throw new Exception($"Can't find line content to validate word");
			}
			ValidateWord(expectedWord);
		}

		public void FindValidateEOL()
		{
			if (FindLineContent())
			{
				throw new Exception($"End of line is expected but something was found");
			}
		}

		public int ReadInt()
		{
			string word = ReadWord();
			if (int.TryParse(word, out int value))
			{
				return value;
			}
			else
			{
				throw new Exception($"Can't parse int value '{word}'");
			}
		}

		public int FindReadInt()
		{
			bool found = FindContent();
			if (!found)
			{
				throw new Exception($"Can't find content to read int");
			}
			return ReadInt();
		}

		public int FindReadLineInt()
		{
			bool found = FindLineContent();
			if (!found)
			{
				throw new Exception($"Can't find line content to read int");
			}
			return ReadInt();
		}

		public int PeekIndend()
		{
			StartPeeking();
			int count = 0;
			while (ReadChar() == TabCharacter)
			{
				count++;
			}
			FinishPeeking();
			return count;
		}

		protected override void ReadWordContent(char c)
		{
			if (c == SlashCharacter)
			{
				if (!TryReadCommentContent())
				{
					base.ReadWordContent(c);
				}
			}
			else if (c == LessCharacter)
			{
				if (!TryReadInheritanceContent())
				{
					base.ReadWordContent(c);
				}
			}
			else
			{
				base.ReadWordContent(c);
			}
		}

		private bool TryReadCommentContent()
		{
			StartPeeking();
			ReadChar();
			if (EndOfStream)
			{
				FinishPeeking();
				return false;
			}

			char nc = PeekChar();
			FinishPeeking();

			// '//'
			if (nc == SlashCharacter)
			{
				ReadAppendChar();
				ReadAppendChar();
				return true;
			}
			else
			{
				return false;
			}
		}

		private bool TryReadInheritanceContent()
		{
			StartPeeking();
			ReadChar();
			if (EndOfStream)
			{
				FinishPeeking();
				return false;
			}

			char nc = PeekChar();
			FinishPeeking();

			// '<-'
			if (nc == MinusCharacter)
			{
				ReadAppendChar();
				ReadAppendChar();
				return true;
			}
			else
			{
				return false;
			}
		}

		private static readonly char[] DumpDelimiters = new char[]
		{
			OpenBraceCharacter, CloseBraceCharacter,
		};

		private const char OpenBraceCharacter = '{';
		private const char CloseBraceCharacter = '}';
		private const char LessCharacter = '<';
	}
}
