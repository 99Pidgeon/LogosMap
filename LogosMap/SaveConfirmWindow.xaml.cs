using System.Windows;
using LogosMap.resources.lang;

namespace LogosMap
{
    public partial class SaveConfirmWindow : Window
    {
        public SaveConfirmWindow()
        {
            InitializeComponent();

            SavePrompt.Title = Strings.Warning;
            Prompt.Text = Strings.SavePrompt;
            SaveButton.Content = Strings.Save;
            DontSaveButton.Content = Strings.DontSave;
            CancelButton.Content = Strings.Cancel;
        }

        public MessageBoxResult Result { get; private set; } = MessageBoxResult.Cancel;

        private void OnYes(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.Yes;
            DialogResult = true;
        }

        private void OnNo(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.No;
            DialogResult = true;
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.Cancel;
            DialogResult = false;
        }
    }
}
