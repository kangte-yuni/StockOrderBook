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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Client.Views
{
    /// <summary>
    /// Interaction logic for OrderBookPanelView.xaml
    /// </summary>
    public partial class OrderBookPanelView : UserControl
    {
        public OrderBookPanelView()
        {
            InitializeComponent();
            DepthScroll.LayoutUpdated += DepthScroll_LayoutUpdated;
        }

        private void DepthScroll_LayoutUpdated(object sender, EventArgs e)
        {
            // 스크롤 가능한 높이가 0보다 커질 때 = 콘텐츠 크기가 계산 완료된 시점
            if (DepthScroll.ScrollableHeight > 0)
            {
                // 핸들러 제거(한 번만 실행)
                DepthScroll.LayoutUpdated -= DepthScroll_LayoutUpdated;

                // 중간으로 이동
                var middle = DepthScroll.ScrollableHeight / 2.0;
                DepthScroll.ScrollToVerticalOffset(middle);
            }
        }
    }
}
