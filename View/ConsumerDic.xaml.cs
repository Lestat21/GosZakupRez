using GosZakup.ViewModel;
using System.Linq;
using System.Windows;

namespace GosZakup.View
{
    public partial class ConsumerDic : Window
    {
        ViewConsumerDic viewConsumerDic;
        public ConsumerDic()
        {
            InitializeComponent();
        }

        public ConsumerDic(ViewConsumerDic viewConsumerDic) : this()
        {
            this.viewConsumerDic = viewConsumerDic;
            var result = from p in viewConsumerDic.ConsumerDics() select p;
            grid_ConsumerDic.ItemsSource = result.ToList();
        }
    }
}
