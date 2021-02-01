using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;

namespace Observatory.UI.Controls
{
    public class TableSizeSelectionGridCell : ButtonBase
    {
        public static DependencyProperty IsSelectedProperty { get; } =
            DependencyProperty.Register(nameof(IsSelected), typeof(bool), typeof(TableSizeSelectionGridCell), new PropertyMetadata(false));

        public bool IsSelected
        {
            get { return (bool)GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }

        public int RowIndex { get; }

        public int ColumnIndex { get; }

        public TableSizeSelectionGridCell(int rowIndex, int columnIndex)
        {
            RowIndex = rowIndex;
            ColumnIndex = columnIndex;
        }

        public TableSizeSelectionGridCell() : this(0, 0) { }
    }
}
