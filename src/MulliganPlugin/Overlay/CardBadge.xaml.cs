using System.Windows.Controls;
using HstMulligan.Core.Models;
using HstMulligan.Plugin.Overlay.ViewModels;

namespace HstMulligan.Plugin.Overlay
{
    public partial class CardBadge : UserControl
    {
        private readonly CardBadgeViewModel _vm = new CardBadgeViewModel();

        public CardBadge()
        {
            InitializeComponent();
            DataContext = _vm;
        }

        public void Update(MulliganAdvice advice, double diameter)
        {
            Width = diameter;
            Height = diameter;
            Backdrop.Width = diameter;
            Backdrop.Height = diameter;
            var innerRatio = 0.62;
            InnerHole.Width = diameter * innerRatio;
            InnerHole.Height = diameter * innerRatio;
            _vm.UpdateFrom(advice, diameter);
            Opacity = advice.HasData ? 1.0 : 0.65;
        }
    }
}
