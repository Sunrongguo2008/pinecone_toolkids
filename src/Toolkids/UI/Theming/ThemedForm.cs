using System;
using System.Windows.Forms;

namespace Toolkids.UI.Theming
{
    /// <summary>
    /// 所有窗体的基类：
    /// 1) 开启按字体的 DPI 自适应；
    /// 2) 在 <see cref="OnLoad"/> 与 <see cref="OnShown"/> 两个时机套用当前主题。
    ///    OnLoad 保证首帧就是深色（不闪白）；OnShown 在 DPI 缩放/显示完成之后再套一次，
    ///    修掉“缩放过程把文本框/窗体颜色冲回浅色”的问题。
    /// </summary>
    public class ThemedForm : Form
    {
        protected ThemedForm()
        {
            AutoScaleMode = AutoScaleMode.Font;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            ThemeManager.Apply(this, AppTheme.Current);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            ThemeManager.Apply(this, AppTheme.Current);
        }
    }
}
