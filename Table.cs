using System.Collections.Generic;
using System.IO;

namespace ASRT_BoostLeagueAssistant
{
    public class Table
    {
        readonly Dictionary<(int, int), string> table = new();
        public int ColumnCount { get; private set; }
        public int RowCount { get; private set; }

        public Table() { }

        public Table(string s)
        {
            string[] rows = s.Split('\n');
            int nRows = rows.Length;
            for (int i = 0; i < nRows; i++)
            {
                string[] cells = rows[i].Split('\t');
                int nCols = cells.Length;
                for (int j = 0; j < nCols; j++)
                {
                    this[i, j] = cells[j];
                }
            }
        }

        public string this[int row, int column]
        {
            get => table.TryGetValue((row, column), out string s) ? s : "";
            set
            {
                table[(row, column)] = value;
                if (row >= RowCount)
                {
                    RowCount = row + 1;
                }
                if (column >= ColumnCount)
                {
                    ColumnCount = column + 1;
                }
            }
        }

        public void ToFile(string path)
        {
            using FileStream fs = File.Create(path);
            using StreamWriter sw = new(fs);
            for (int i = 0; i < RowCount; i++)
            {
                for (int j = 0; j < ColumnCount; j++)
                {
                    if (j > 0)
                    {
                        sw.Write('\t');
                    }
                    string text = this[i, j];
                    if (!string.IsNullOrEmpty(text))
                    {
                        sw.Write(text);
                    }
                }
                sw.WriteLine();
            }
        }
    }
}