using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Observatory.UI.Controls
{
    public class TableSizeSelectionGrid : Control
    {
        private const int ROW_COUNT = 10;
        private const int COLUMN_COUNT = 10;
        private const string DEFAULT_DISPLAY_SIZE = "Insert table";

        public static DependencyProperty CellsProperty { get; } =
            DependencyProperty.Register(nameof(Cells), typeof(IReadOnlyList<TableSizeSelectionGridCell>),
                typeof(TableSizeSelectionGrid), new PropertyMetadata(null));

        public static DependencyProperty DisplaySizeProperty { get; } =
            DependencyProperty.Register(nameof(DisplaySize), typeof(string),
                typeof(TableSizeSelectionGrid), new PropertyMetadata(DEFAULT_DISPLAY_SIZE));

        public IReadOnlyList<TableSizeSelectionGridCell> Cells
        {
            get => (IReadOnlyList<TableSizeSelectionGridCell>)GetValue(CellsProperty);
            private set => SetValue(CellsProperty, value);
        }

        public string DisplaySize
        {
            get { return (string)GetValue(DisplaySizeProperty); }
            private set { SetValue(DisplaySizeProperty, value); }
        }

        public TableSizeSelectionGridCell this[int rowIndex, int columnIndex] => Cells[rowIndex * COLUMN_COUNT + columnIndex];

        public event EventHandler<TableSizeSelectionEventArgs> SizeSelected;

        private TableSizeSelectionGridCell _lastHoveredCell = null;

        public TableSizeSelectionGrid()
        {
            var cells = new List<TableSizeSelectionGridCell>(ROW_COUNT * COLUMN_COUNT);
            for (var i = 0; i < ROW_COUNT; ++i)
            {
                for (var j = 0; j < COLUMN_COUNT; ++j)
                {
                    var cell = new TableSizeSelectionGridCell(i, j);
                    cell.PointerEntered += OnCellPointerEntered;
                    cell.Click += OnCellClicked;
                    cells.Add(cell);
                }
            }
            Cells = cells.AsReadOnly();
            Unloaded += OnUnloaded;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _lastHoveredCell = null;
            DisplaySize = DEFAULT_DISPLAY_SIZE;
            for (var i = 0; i < ROW_COUNT; ++i)
            {
                for (var j = 0; j < COLUMN_COUNT; ++j)
                {
                    this[i, j].IsSelected = false;
                }
            }
        }

        private void OnCellClicked(object sender, RoutedEventArgs e)
        {
            var cell = (TableSizeSelectionGridCell)sender;
            SizeSelected?.Invoke(this, new TableSizeSelectionEventArgs(cell.RowIndex + 1, cell.ColumnIndex + 1));
        }

        public void OnCellPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            var cell = (TableSizeSelectionGridCell)sender;
            var rowIndex = cell.RowIndex;
            var columnIndex = cell.ColumnIndex;

            if (_lastHoveredCell == null)
            {
                for (var i = 0; i <= rowIndex; ++i)
                {
                    for (var j = 0; j <= columnIndex; ++j)
                    {
                        this[i, j].IsSelected = true;
                    }
                }
            }
            else
            {
                if (_lastHoveredCell.RowIndex > rowIndex)
                {
                    for (var i = rowIndex + 1; i <= _lastHoveredCell.RowIndex; ++i)
                    {
                        for (var j = 0; j <= _lastHoveredCell.ColumnIndex; ++j)
                        {
                            this[i, j].IsSelected = false;
                        }
                    }
                }
                else if (_lastHoveredCell.RowIndex < rowIndex)
                {
                    for (var i = _lastHoveredCell.RowIndex + 1; i <= rowIndex; ++i)
                    {
                        for (var j = 0; j <= columnIndex; ++j)
                        {
                            this[i, j].IsSelected = true;
                        }
                    }
                }

                if (_lastHoveredCell.ColumnIndex > columnIndex)
                {
                    for (var i = 0; i <= _lastHoveredCell.RowIndex; ++i)
                    {
                        for (var j = columnIndex + 1; j <= _lastHoveredCell.ColumnIndex; ++j)
                        {
                            this[i, j].IsSelected = false;
                        }
                    }
                }
                else if (_lastHoveredCell.ColumnIndex < columnIndex)
                {
                    for (var i = 0; i <= rowIndex; ++i)
                    {
                        for (var j = _lastHoveredCell.ColumnIndex + 1; j <= columnIndex; ++j)
                        {
                            this[i, j].IsSelected = true;
                        }
                    }
                }
            }
            _lastHoveredCell = this[rowIndex, columnIndex];
            DisplaySize = $"{rowIndex + 1} x {columnIndex + 1}";
        }
    }
}
