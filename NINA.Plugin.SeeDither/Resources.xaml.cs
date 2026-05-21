using System.Windows;
using System.Windows.Input;

namespace NINA.Plugin.SeeDither {
    public partial class Resources : ResourceDictionary {
        public Resources() {
            InitializeComponent();
        }

        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                var tb = sender as System.Windows.Controls.TextBox;
                tb?.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty)?.UpdateSource();
                tb?.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                e.Handled = true;
            }
        }
    }
}
