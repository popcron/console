using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Popcron.Console
{
    /// <summary>
    /// Utility class for creating a stringified table to print.
    /// </summary>
    public class Table
    {
        public const char HeaderSeparator = '=';
        public const char ColumnSeparator = '|';
        public const string NullString = "null";
        public const string TrueString = "✔";
        public const string FalseString = "✕";

        private static StringBuilder builder = new StringBuilder();
        private const string Indent = "    ";
        private const int Padding = 5;

        private int columns;
        private int rows;
        private List<object[]> cells;
        private Dictionary<string, int> richTextOffset;

        private Table()
        {

        }

        public Table(params object[] row)
        {
            if (row != null && row.Length != 0)
            {
                cells = new List<object[]>();
                richTextOffset = new Dictionary<string, int>();
                columns = row.Length;
                rows = 1;
                cells.Add(row);
            }
            else
            {
                throw new Exception($"Creating a table with no columns is not allowed");
            }
        }

        /// <summary>
        /// Inserts a new row to the table at this index.
        /// Must match the original amount of columns provided.
        /// </summary>
        public void InsertRow(int index, params object[] row)
        {
            if (row.Length == columns)
            {
                cells.Insert(index + 1, row);
                rows++;
            }
            else
            {
                throw new Exception($"Column count does not match, must be {columns} column(s)");
            }
        }

        /// <summary>
        /// Adds a new row to the bottom of the table.
        /// Must match the original amount of columns provided.
        /// </summary>
        public void AddRow(params object[] row)
        {
            if (row.Length == columns)
            {
                cells.Add(row);
                rows++;
            }
            else
            {
                throw new Exception($"Column count does not match, must be {columns} column(s)");
            }
        }

        public override string ToString()
        {
            builder.Clear();

            //gather all texts and max lengths
            string[,] texts = new string[columns, rows];
            int[] maxLengths = new int[columns];
            int totalWidth = Indent.Length;
            for (int c = 0; c < columns; c++)
            {
                int maxLength = 0;
                for (int r = 0; r < rows; r++)
                {
                    object cell = cells[r][c];
                    string text;
                    int length = 0;
                    if (cell is bool)
                    {
                        text = ((bool)cell) ? TrueString : FalseString;
                        richTextOffset[text] = 0;

                        if (!string.IsNullOrEmpty(text))
                        {
                            length = text.Length;
                        }
                    }
                    else
                    {
                        if (cell is null)
                        {
                            text = NullString;
                            richTextOffset[text] = 0;

                            if (!string.IsNullOrEmpty(text))
                            {
                                length = text.Length;
                            }
                        }
                        else
                        {
                            text = cell.ToString();
                            if (!string.IsNullOrEmpty(text))
                            {
                                int richTextCount = CountRichTextLength(text);
                                length = text.Length - richTextCount;
                                richTextOffset[text] = richTextCount;
                            }
                            else
                            {
                                richTextOffset[text] = 0;
                            }
                        }
                    }

                    texts[c, r] = text;
                    maxLength = Mathf.Max(maxLength, length);
                }

                maxLengths[c] = maxLength;
            }

            //calculate total width of the table
            for (int c = 0; c < columns; c++)
            {
                totalWidth += maxLengths[c] + Padding;
            }

            //build text
            for (int r = 0; r < rows; r++)
            {
                builder.Append(Indent);
                for (int c = 0; c < columns; c++)
                {
                    string text = texts[c, r];
                    int totalLength = maxLengths[c] + Padding;

                    if (text != null)
                    {
                        AppendPadRight(text, totalLength + richTextOffset[text]);
                        if (c < columns - 1)
                        {
                            builder.Append(ColumnSeparator);
                            builder.Append(' ');
                        }
                    }
                    else
                    {
                        AppendRepeatingString(' ', totalLength);
                        if (c < columns - 1)
                        {
                            builder.Append(ColumnSeparator);
                            builder.Append(' ');
                        }
                    }
                }

                if (r == 0)
                {
                    //header row
                    builder.AppendLine();
                    AppendRepeatingString(HeaderSeparator, totalWidth);
                    builder.AppendLine();
                }
                else
                {
                    builder.AppendLine();
                }
            }

            return builder.ToString();
        }

        private int CountRichTextLength(string text)
        {
            int count = 0;
            int textLength = text.Length;
            bool insideTag = false;
            for (int i = 0; i < textLength; i++)
            {
                char c = text[i];
                if (c == '<')
                {
                    insideTag = true;
                }

                if (insideTag)
                {
                    count++;
                }

                if (c == '>')
                {
                    insideTag = false;
                }
            }

            return count;
        }

        private void AppendPadRight(string text, int totalWidth)
        {
            builder.Append(text);
            if (text.Length < totalWidth)
            {
                int remainder = totalWidth - text.Length;
                AppendRepeatingString(' ', remainder);
            }
        }

        private void AppendRepeatingString(char c, int length)
        {
            for (int i = length - 1; i >= 0; i--)
            {
                builder.Append(c);
            }
        }
    }
}