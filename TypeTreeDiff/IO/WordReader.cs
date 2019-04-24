using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TypeTreeDiff
{
	public class WordReader : IDisposable
	{
		public WordReader(Stream stream) :
			this(stream, WordDelimiters)
		{
		}

		protected WordReader(Stream stream, IEnumerable<char> delimiters)
		{
			m_baseStream = stream ?? throw new ArgumentNullException(nameof(stream));
			if (!stream.CanRead)
			{
				throw new ArgumentException("Stream can't read", nameof(stream));
			}
			if (!stream.CanSeek)
			{
				throw new ArgumentException("Stream can't seek", nameof(stream));
			}
			m_binaryReader = new BinaryReader(stream, Encoding.UTF8);

			foreach (char delimiter in delimiters)
			{
				m_delimiters.Add(delimiter);
			}
		}

		~WordReader()
		{
			Dispose(false);
		}

		public string PeekWord()
		{
			if (EndOfStream)
			{
				throw new Exception("Can't peek word. EOF");
			}

			StartPeeking();
			ReadWordContent();
			FinishPeeking();

			string word = m_word.ToString();
			m_word.Clear();
			return word;
		}

		public string ReadWord()
		{
			if (EndOfStream)
			{
				throw new Exception("Can't read word. EOF");
			}

			ReadWordContent();

			string word = m_word.ToString();
			m_word.Clear();
			return word;
		}

		/// <summary>
		/// Find next readable character in the whole stream
		/// </summary>
		/// <returns>True if readable character is found. False if EOF</returns>
		public bool FindContent()
		{
			return FindContent(false);
		}

		/// <summary>
		/// Find next readable character in the current line
		/// </summary>
		/// <returns>True if readable character is found. False if either EOF or EOL</returns>
		public bool FindLineContent()
		{
			return FindContent(true);
		}

		/// <summary>
		/// Find next line
		/// </summary>
		/// <returns>True if next line is found. False if EOF</returns>
		public bool FindNextLine()
		{
			while (true)
			{
				if (EndOfStream)
				{
					return false;
				}

				char c = ReadChar();
				if (EndOfStream)
				{
					return false;
				}

				if (c == CRCharacter)
				{
					char nc = PeekChar();
					if (nc == LFCharacter)
					{
						ReadChar();
						return !EndOfStream;
					}
					else
					{
						return true;
					}
				}
				else if (c == LFCharacter)
				{
					return true;
				}
			}
		}

		public string FindPeekWord()
		{
			bool isFound = FindContent();
			if (!isFound)
			{
				throw new Exception($"Can't find word for peeking");
			}

			return PeekWord();
		}

		public string FindReadWord()
		{
			bool isFound = FindContent();
			if (!isFound)
			{
				throw new Exception($"Can't find word for reading");
			}

			return ReadWord();
		}

		public void Dispose()
		{
			GC.SuppressFinalize(this);
			Dispose(true);
		}

		public void StartPeeking()
		{
			m_peekPositions.Push(m_baseStream.Position);
			if (m_peekPositions.Count > 3)
			{
				throw new Exception("State/Finish mismatch?");
			}
		}

		public void FinishPeeking()
		{
			m_baseStream.Position = m_peekPositions.Pop();
		}

		protected virtual void Dispose(bool disposing)
		{
			m_binaryReader.Dispose();
			m_baseStream.Dispose();
		}

		/// <summary>
		/// Returns the next available character and does not advance position
		/// </summary>
		/// <returns>The next available character, or -1 if no more characters are available</returns>
		protected char PeekChar()
		{
			return unchecked((char)m_binaryReader.PeekChar());
		}

		/// <summary>
		/// Reads the next character from the current stream and advances the current position of the stream in accordance with the
		/// <see langword="Encoding" /> used and the specific character being read from the stream.
		/// Updates Line and Column number and Line content if <see cref="IsAdvancing"/> is set
		/// </summary>
		/// <returns>A character read from the current stream.</returns>
		protected char ReadChar()
		{
			char c = m_binaryReader.ReadChar();
			if (IsAdvancing)
			{
				if (c == CRCharacter)
				{
					ColumnNumber = StartColumnNumber;
					LineNumber++;
					m_line.Clear();
					m_isCR = true;
				}
				else if (c == LFCharacter)
				{
					if (!m_isCR)
					{
						ColumnNumber = StartColumnNumber;
						LineNumber++;
					}
					m_line.Clear();
					m_isCR = false;
				}
				else
				{
					if (c == TabCharacter)
					{
						ColumnNumber += (4 - (ColumnNumber - 1) % 4);
					}
					else
					{
						ColumnNumber++;
					}
					m_isCR = false;
					m_line.Append(c);
				}
			}
			return c;
		}

		protected char ReadAppendChar()
		{
			char c = ReadChar();
			m_word.Append(c);
			return c;
		}

		protected void ReadWordContent()
		{
			char c = PeekChar();
			switch (c)
			{
				case SpaceCharacter:
				case TabCharacter:
				case CRCharacter:
				case LFCharacter:
					return;
			}

			ReadWordContent(c);
		}

		protected virtual void ReadWordContent(char c)
		{
			if (m_delimiters.Contains(c))
			{
				ReadAppendChar();
			}
			else
			{
				ReadGenericWordContent();
			}
		}

		protected void ReadGenericWordContent()
		{
			while (true)
			{
				ReadAppendChar();
				if (EndOfStream)
				{
					return;
				}

				char nc = PeekChar();
				if (m_delimiters.Contains(nc))
				{
					return;
				}
			}
		}

		protected void ReadLineContent()
		{
			while (true)
			{
				if (EndOfStream)
				{
					return;
				}

				char c = PeekChar();
				if (c == CRCharacter || c == LFCharacter)
				{
					return;
				}

				ReadAppendChar();
			}
		}

		protected char GetLastWordChar()
		{
			return m_word[m_word.Length - 1];
		}

		private bool FindContent(bool isLine)
		{
			while (true)
			{
				if (EndOfStream)
				{
					return false;
				}

				char c = PeekChar();
				switch (c)
				{
					case SpaceCharacter:
					case TabCharacter:
						ReadChar();
						continue;

					case CRCharacter:
					case LFCharacter:
						if (isLine)
						{
							return false;
						}
						ReadChar();
						continue;
				}

				return true;
			}
		}

		public bool EndOfStream => m_baseStream.Position == m_baseStream.Length;
		public int LineNumber { get; private set; } = StartLineNumber;
		public int ColumnNumber { get; private set; } = StartColumnNumber;
		public string Line => m_line.ToString();

		protected long BaseStreamPosition => m_baseStream.Position;
		protected bool IsAdvancing => m_peekPositions.Count == 0;

		protected static readonly char[] WordDelimiters = new char[]
		{
			SpaceCharacter, TabCharacter, CRCharacter, LFCharacter,
			DotCharacter, CommaCharacter, ColonCharacter, SemicolonCharacter, QuestionCharacter, ExclamationCharacter,
			DoubleQuoteCharacter, OpenParenthesCharacter, CloseParenthesCharacter,
			SlashCharacter, MinusCharacter, AmpersandCharacter,
		};

		protected const char SpaceCharacter = ' ';
		protected const char TabCharacter = '\t';
		protected const char CRCharacter = '\r';
		protected const char LFCharacter = '\n';

		protected const char DoubleQuoteCharacter = '"';
		protected const char OpenParenthesCharacter = '(';
		protected const char CloseParenthesCharacter = ')';
		protected const char SlashCharacter = '/';
		protected const char MinusCharacter = '-';
		protected const char AmpersandCharacter = '&';
		protected const char DotCharacter = '.';
		protected const char CommaCharacter = ',';
		protected const char ColonCharacter = ':';
		protected const char SemicolonCharacter = ';';
		protected const char QuestionCharacter = '?';
		protected const char ExclamationCharacter = '!';

		private const int StartLineNumber = 1;
		private const int StartColumnNumber = 1;

		private readonly HashSet<char> m_delimiters = new HashSet<char>();
		private readonly Stack<long> m_peekPositions = new Stack<long>();

		private readonly Stream m_baseStream;
		private readonly BinaryReader m_binaryReader;
		private readonly StringBuilder m_word = new StringBuilder();
		private readonly StringBuilder m_line = new StringBuilder();
		private bool m_isCR;
	}
}