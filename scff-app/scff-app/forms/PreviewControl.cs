﻿using System.Windows.Forms;
using System.Drawing;

namespace scff_app.gui {

public partial class PreviewControl : UserControl {
  private MovableAndResizable drag_mover_;
  private LayoutParameter layout_parameter_;

  private Font info_font_;
  private PointF info_point_f_;
  private string info_;

  public int IndexInLayoutParameterBindingSource { get; set; }

  public PreviewControl(int bound_width, int bound_height,
                        int index_in_layout_parameter_binding_source,
                        LayoutParameter layout_parameter) {
    InitializeComponent();

    // メンバの設定
    layout_parameter_ = layout_parameter;
    IndexInLayoutParameterBindingSource = index_in_layout_parameter_binding_source;

    info_font_ = new Font("Verdana", 10, FontStyle.Bold);
    info_point_f_ = new PointF(0, 0);
    info_ = IndexInLayoutParameterBindingSource.ToString() + ": " + layout_parameter_.WindowText;

    drag_mover_ = new MovableAndResizable(this, bound_width, bound_height);
  }

  private void innerPanel_MouseDown(object sender, MouseEventArgs e) {
    OnMouseDown(e);
  }

  private void innerPanel_MouseMove(object sender, MouseEventArgs e) {
    OnMouseMove(e);
  }

  private void innerPanel_MouseUp(object sender, MouseEventArgs e) {
    OnMouseUp(e);
  }

  private void inner_panel_Paint(object sender, PaintEventArgs e) {
    e.Graphics.DrawString(info_, info_font_, Brushes.DarkOrange, info_point_f_);
  }
}
}
