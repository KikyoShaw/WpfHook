using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WpfHook.ViewModel;
using MessageBox = System.Windows.MessageBox;

namespace WpfHook
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += MainWindow_Loaded;
        }

        /// <summary>
        /// 声明一个hook对象
        /// </summary>
        private HookHelper hook;

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            BtnInstallHook.IsEnabled = true;
            BtnUnInstall.IsEnabled = false;
            //初始化钩子对象
            hook ??= new HookHelper();
            hook.KeyDown += new KeyEventHandler(Hook_KeyDown);
            //hook.KeyPress += new KeyPressEventHandler(Hook_KeyPress);
            hook.KeyUp += new KeyEventHandler(Hook_KeyUp);
            hook.OnMouseActivity += new MouseEventHandler(Hook_OnMouseActivity);
        }

        private void SetHookButton_clicked(object sender, RoutedEventArgs e)
        {
            if (BtnInstallHook.IsEnabled)
            {
                bool r = hook.Start();
                if (r)
                {
                    BtnInstallHook.IsEnabled = false;
                    BtnUnInstall.IsEnabled = true;
                    MessageBox.Show("安装钩子成功!");
                }
                else
                {
                    MessageBox.Show("安装钩子失败!");
                }
            }
        }

        private void UnHookButton_clicked(object sender, RoutedEventArgs e)
        {
            if (BtnUnInstall.IsEnabled)
            {
                hook.Stop();
                BtnUnInstall.IsEnabled = false;
                BtnInstallHook.IsEnabled = true;
                MessageBox.Show("卸载钩子成功!");
            }
        }

        /// <summary>
        /// 鼠标移动事件
        /// </summary>
        void Hook_OnMouseActivity(object sender, MouseEventArgs e)
        {
            MouseText.Text = "X:" + e.X + " Y:" + e.Y;
        }
        /// <summary>
        /// 键盘抬起
        /// </summary>
        void Hook_KeyUp(object sender, KeyEventArgs e)
        {
            KeyText.Text = "键盘抬起, " + e.KeyData.ToString() + " 键码:" + e.KeyValue;
        }
        /// <summary>
        /// 键盘输入
        /// </summary>
        void Hook_KeyPress(object sender, KeyPressEventArgs e)
        { }
        /// <summary>
        /// 键盘按下
        /// </summary>
        void Hook_KeyDown(object sender, KeyEventArgs e)
        {
            KeyText.Text = "键盘按下, " + e.KeyData.ToString() + " 键码:" + e.KeyValue;
        }
    }
}
