namespace Govor.Mobile.Pages.Base;

public partial class AdaptivePage : ContentPage
{
    /// <summary>
    /// ����������� ������ ���� ��� �������� � "�������" �����.
    /// </summary>
    protected virtual double WideThreshold => 700;

    private bool _isWide = false;

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        
#if WINDOWS_UWP
        // ���� ��������� ���������� � �������������
        bool nowWide = width > WideThreshold;

        if (nowWide != _isWide)
        {
            _isWide = nowWide;
            if (nowWide)
                OnSwitchToWide();
            else
                OnSwitchToNarrow();
        }
#endif
    }

    /// <summary>
    /// �����������, ����� ������ ��������� WideThreshold.
    /// </summary>
    protected virtual void OnSwitchToWide() { }

    /// <summary>
    /// �����������, ����� ������ ���������� ������ WideThreshold.
    /// </summary>
    protected virtual void OnSwitchToNarrow() { }
}