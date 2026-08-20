using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Membrane.CapeOpen
{
    /// <summary>
    /// Modal Windows Forms editor shown from <see cref="MembraneUnitOperation.Edit"/>. Every INPUT parameter gets
    /// a native control: text boxes for real values, drop-down lists for the option parameters, and a grid for the
    /// per-compound permeances. This is mainly valuable in DWSIM, whose CAPE-OPEN parameter grid renders option
    /// parameters as plain text cells (no drop-down); COFE already offers drop-downs in its own grid, so there this
    /// simply mirrors it. WinForms is used because it matches native COFE/COCO dialogs and DWSIM's own UI is WinForms.
    ///
    /// The form edits the actual <see cref="ICapeParameter"/> objects on OK; MembraneArea vs StageCut editability
    /// follows the SpecMode selection (the unit's ApplySpecMode flips their input/output roles).
    /// </summary>
    internal sealed class MembraneEditForm : Form
    {
        private readonly RealParameter _pp, _area, _cut;
        private readonly OptionParameter _flow, _spec, _energy, _driving;
        private readonly IDictionary<string, RealParameter> _permeances;
        private readonly string _stageCutSpecValue;
        private readonly Action _applySpecMode;

        private ComboBox _cFlow = null!, _cSpec = null!, _cEnergy = null!, _cDriving = null!;
        private TextBox _tPP = null!, _tArea = null!, _tCut = null!;
        private DataGridView _grid = null!;

        public MembraneEditForm(
            RealParameter permeatePressure, RealParameter membraneArea, RealParameter stageCut,
            OptionParameter flowPattern, OptionParameter specMode,
            OptionParameter energyMode, OptionParameter drivingForce,
            IDictionary<string, RealParameter> permeances,
            string stageCutSpecValue, Action applySpecMode)
        {
            _pp = permeatePressure; _area = membraneArea; _cut = stageCut;
            _flow = flowPattern; _spec = specMode; _energy = energyMode; _driving = drivingForce;
            _permeances = permeances; _stageCutSpecValue = stageCutSpecValue; _applySpecMode = applySpecMode;
            BuildUi();
            LoadValues();
            UpdateSpecEnabled();
        }

        private static string Fmt(double v) => v.ToString("G6", CultureInfo.InvariantCulture);

        private void BuildUi()
        {
            Text = "Membrane (Gas Permeation, Cross-Flow) — configuration";
            Font = SystemFonts.MessageBoxFont;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false; MaximizeBox = false; ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(480, 560);

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(12) };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var fields = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 2, AutoSize = true };
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            _cFlow = AddCombo(fields, "Flow pattern", _flow);
            _cSpec = AddCombo(fields, "Specification mode", _spec);
            _tArea = AddText(fields, "Membrane area [m²]");
            _tCut = AddText(fields, "Stage cut [–]");
            _tPP = AddText(fields, "Permeate pressure [Pa]");
            _cEnergy = AddCombo(fields, "Energy balance", _energy);
            _cDriving = AddCombo(fields, "Driving force", _driving);
            _cSpec.SelectedIndexChanged += (s, e) => UpdateSpecEnabled();

            var group = new GroupBox
            {
                Text = "Per-compound permeance [mol·m⁻²·s⁻¹·Pa⁻¹]",
                Dock = DockStyle.Fill, Padding = new Padding(6)
            };
            _grid = new DataGridView
            {
                Dock = DockStyle.Fill, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false, RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2, BackgroundColor = SystemColors.Window,
                SelectionMode = DataGridViewSelectionMode.CellSelect
            };
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Compound", ReadOnly = true, FillWeight = 45 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Permeance", FillWeight = 55 });
            group.Controls.Add(_grid);

            var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Margin = new Padding(0, 8, 0, 0) };
            var ok = new Button { Text = "OK", AutoSize = true, Padding = new Padding(14, 2, 14, 2) };
            var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true, Padding = new Padding(10, 2, 10, 2) };
            ok.Click += OnOk;
            buttons.Controls.Add(ok);
            buttons.Controls.Add(cancel);
            AcceptButton = ok; CancelButton = cancel;

            root.Controls.Add(fields, 0, 0);
            root.Controls.Add(group, 0, 1);
            root.Controls.Add(buttons, 0, 2);
            Controls.Add(root);
        }

        private static Label MakeLabel(string text) =>
            new Label { Text = text, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 7, 6, 0) };

        private ComboBox AddCombo(TableLayoutPanel t, string label, OptionParameter p)
        {
            var cb = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Anchor = AnchorStyles.Left | AnchorStyles.Right, Margin = new Padding(0, 4, 0, 4) };
            foreach (var o in (string[])p.OptionList) cb.Items.Add(o);
            int r = t.RowCount++;
            t.Controls.Add(MakeLabel(label), 0, r);
            t.Controls.Add(cb, 1, r);
            return cb;
        }

        private TextBox AddText(TableLayoutPanel t, string label)
        {
            var tb = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right, Margin = new Padding(0, 4, 0, 4) };
            int r = t.RowCount++;
            t.Controls.Add(MakeLabel(label), 0, r);
            t.Controls.Add(tb, 1, r);
            return tb;
        }

        private void LoadValues()
        {
            _cFlow.SelectedItem = _flow.ValueCore;
            _cSpec.SelectedItem = _spec.ValueCore;
            _cEnergy.SelectedItem = _energy.ValueCore;
            _cDriving.SelectedItem = _driving.ValueCore;
            _tPP.Text = Fmt(_pp.ValueCore);
            _tArea.Text = Fmt(_area.ValueCore);
            _tCut.Text = Fmt(_cut.ValueCore);
            foreach (var kv in _permeances)
                _grid.Rows.Add(kv.Key, Fmt(kv.Value.ValueCore));
            if (_permeances.Count == 0)
            {
                int r = _grid.Rows.Add("(connect a feed, then re-open)", "");
                _grid.Rows[r].ReadOnly = true;
            }
        }

        private bool StageCutSelected => (_cSpec.SelectedItem?.ToString() ?? "") == _stageCutSpecValue;

        private void UpdateSpecEnabled()
        {
            bool sc = StageCutSelected;
            _tArea.Enabled = !sc; _tArea.BackColor = sc ? SystemColors.Control : SystemColors.Window;
            _tCut.Enabled = sc;   _tCut.BackColor  = sc ? SystemColors.Window  : SystemColors.Control;
        }

        private bool TryParseReal(TextBox tb, RealParameter p, string name, out double v, out string? err)
        {
            err = null;
            if (!double.TryParse(tb.Text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out v))
            { err = $"{name}: ‘{tb.Text}’ is not a number."; return false; }
            if (v < p.LowerBoundCore || v > p.UpperBoundCore)
            { err = $"{name}: {Fmt(v)} is outside [{Fmt(p.LowerBoundCore)}, {Fmt(p.UpperBoundCore)}]."; return false; }
            return true;
        }

        private void OnOk(object? sender, EventArgs e)
        {
            var errors = new List<string>();
            bool sc = StageCutSelected;

            double pp = 0, area = 0, cut = 0;
            if (!TryParseReal(_tPP, _pp, "Permeate pressure", out pp, out var ep)) errors.Add(ep!);
            if (sc) { if (!TryParseReal(_tCut, _cut, "Stage cut", out cut, out var ec)) errors.Add(ec!); }
            else    { if (!TryParseReal(_tArea, _area, "Membrane area", out area, out var ea)) errors.Add(ea!); }

            var permVals = new List<KeyValuePair<string, double>>();
            foreach (DataGridViewRow row in _grid.Rows)
            {
                if (row.Cells[0].Value is not string id || !_permeances.TryGetValue(id, out var pParam)) continue;
                string txt = (row.Cells[1].Value as string ?? "").Trim();
                if (!double.TryParse(txt, NumberStyles.Float, CultureInfo.InvariantCulture, out var pv))
                { errors.Add($"Permeance {id}: ‘{txt}’ is not a number."); continue; }
                if (pv < pParam.LowerBoundCore || pv > pParam.UpperBoundCore)
                { errors.Add($"Permeance {id}: {Fmt(pv)} outside [{Fmt(pParam.LowerBoundCore)}, {Fmt(pParam.UpperBoundCore)}]."); continue; }
                permVals.Add(new KeyValuePair<string, double>(id, pv));
            }

            if (errors.Count > 0)
            {
                MessageBox.Show(this, string.Join(Environment.NewLine, errors), "Invalid input",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return; // keep the form open
            }

            // Apply. SpecMode first so ApplySpecMode sets which of area/cut is the INPUT.
            _spec.value = (string)_cSpec.SelectedItem!;
            _applySpecMode();
            _flow.value = (string)_cFlow.SelectedItem!;
            _energy.value = (string)_cEnergy.SelectedItem!;
            _driving.value = (string)_cDriving.SelectedItem!;
            _pp.value = pp;
            if (sc) _cut.value = cut; else _area.value = area;
            foreach (var kv in permVals) _permeances[kv.Key].value = kv.Value;

            DialogResult = DialogResult.OK;
            Close();
        }
    }

    /// <summary>Resolves the host PME's top window so the editor is shown modally over it.</summary>
    internal static class NativeOwner
    {
        [DllImport("user32.dll")] private static extern IntPtr GetActiveWindow();
        [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();

        public static IWin32Window? TryGet()
        {
            IntPtr h = GetActiveWindow();
            if (h == IntPtr.Zero) h = GetForegroundWindow();
            return h == IntPtr.Zero ? null : new Win32Window(h);
        }

        private sealed class Win32Window : IWin32Window
        {
            public Win32Window(IntPtr handle) => Handle = handle;
            public IntPtr Handle { get; }
        }
    }
}
