using System;
using System.Collections.Generic;
using System.Text;

namespace Observatory.UI.Controls
{
    public class TableSizeSelectionEventArgs : EventArgs
    {
        public int RowCount { get; }
        public int ColumnCount { get; }

        public TableSizeSelectionEventArgs(int rowCount, int columnCount)
        {
            RowCount = rowCount;
            ColumnCount = columnCount;
        }
    }
}
