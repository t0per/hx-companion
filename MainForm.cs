namespace HxCompanion;

sealed class MainForm : Form
{
    // Level is a signed dB byte. A Cloud III S Wireless was observed to cap at +6 dB; -6 dB is NGENUITY's own minimum.
    const int SidetoneMinDb = -6, SidetoneMaxDb = 6;

    readonly Headset _headset = new();

    readonly CheckBox _sidetoneOn = new() { Text = "Sidetone activé", AutoSize = true };
    readonly TrackBar _sidetone = new() { Minimum = SidetoneMinDb, Maximum = SidetoneMaxDb, TickFrequency = 1, Width = 300 };
    readonly Label _sidetoneValue = new() { AutoSize = true };
    readonly Label _status = new() { AutoSize = true, MaximumSize = new Size(380, 0), MinimumSize = new Size(380, 48) };
    bool _loading;

    public MainForm()
    {
        Text = "HyperX Companion — Cloud III S Wireless";
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(12);

        var reconnect = new Button { Text = "Reconnecter", AutoSize = true };
        var layout = new TableLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 2, Location = new Point(12, 12) };
        void Row(Control left, Control? right = null)
        {
            layout.Controls.Add(left);
            if (right == null) layout.SetColumnSpan(left, 2); else layout.Controls.Add(right);
        }
        Row(new Label { Text = "MICRO — RETOUR VOIX", AutoSize = true, Font = new Font(Font, FontStyle.Bold) });
        Row(_sidetoneOn);
        Row(_sidetone, _sidetoneValue);
        Row(reconnect);
        Row(_status);
        Controls.Add(layout);

        _sidetoneOn.CheckedChanged += (_, _) => Apply(() => _headset.SetSidetoneEnabled(_sidetoneOn.Checked));
        // Send on release, not on every tick, so the headset is not flooded while dragging.
        _sidetone.ValueChanged += (_, _) => _sidetoneValue.Text = $"{_sidetone.Value:+0;-0;0} dB";
        _sidetone.MouseUp += (_, _) => ApplySidetone();
        _sidetone.KeyUp += (_, _) => ApplySidetone();
        reconnect.Click += (_, _) => LoadState();
        Shown += (_, _) => LoadState();
        FormClosed += (_, _) => _headset.Dispose();
    }

    void LoadState()
    {
        _loading = true;
        try
        {
            _headset.Find();
            _sidetoneOn.Checked = _headset.GetSidetoneEnabled();
            _sidetone.Value = Math.Clamp(_headset.GetSidetoneDb(), _sidetone.Minimum, _sidetone.Maximum);
            _sidetoneOn.Enabled = _sidetone.Enabled = true;
            _status.Text = "Connecté : " + _headset.Name;
        }
        catch (Exception e)
        {
            _sidetoneOn.Enabled = _sidetone.Enabled = false;
            _status.Text = "Casque : " + e.Message;
        }
        _sidetoneValue.Text = $"{_sidetone.Value:+0;-0;0} dB";
        _loading = false;
    }

    void ApplySidetone() => Apply(() =>
    {
        _headset.SetSidetoneDb(_sidetone.Value);
        _status.Text = $"Sidetone demandé {_sidetone.Value} dB, casque : {_headset.GetSidetoneDb()} dB";
    });

    void Apply(Action action)
    {
        if (_loading) return;
        try
        {
            _status.Text = "";
            action();
        }
        catch (Exception e)
        {
            _status.Text = e.Message;
        }
    }
}
