using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

namespace Observatory.UI.Controls
{
    public class OneWayToggleButton : ToggleButton
    {
        private bool _isClicked = false;

        public OneWayToggleButton()
        {
            this.Click += this.OnClicked;
        }
        
        private void OnClicked(object sender, RoutedEventArgs e)
        {
            _isClicked = true;
        }

        protected override void OnPointerExited(PointerRoutedEventArgs e)
        {
            base.OnPointerExited(e);
            if (_isClicked)
            {
                _isClicked = false;
                e.Handled = true;
            }
        }

        protected override void OnToggle()
        {
        }
    }
}
