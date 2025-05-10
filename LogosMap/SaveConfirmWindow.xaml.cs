using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LogosMap
{
    /// <summary>
    /// SaveConfirmWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SaveConfirmWindow : Window
    {
        public SaveConfirmWindow()
        {
            InitializeComponent();
        }

        // 호출 쪽에서 이 값을 보고 분기 처리
        public MessageBoxResult Result { get; private set; } = MessageBoxResult.Cancel;

        private void OnYes(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.Yes;
            DialogResult = true;    // ShowDialog() 리턴값이 true
        }

        private void OnNo(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.No;
            DialogResult = true;
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.Cancel;
            DialogResult = false;   // ShowDialog() 리턴값이 false
        }
    }
}
