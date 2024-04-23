using Emgu.CV.Structure;
using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Emgu.CV.CvEnum;
using System.Runtime.InteropServices;
using System.IO;
using Emgu.CV.Util;
using System.Linq;
using Emgu.CV.Features2D;
using System.Diagnostics;
using System.Threading;
using Point = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;
using TextBox = System.Windows.Forms.TextBox;
using ScrollBars = System.Windows.Forms.ScrollBars;
using Series = System.Windows.Forms.DataVisualization.Charting.Series;
using System.Windows.Forms.DataVisualization.Charting;

namespace Historical_new_final
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            string path_tmp = "";
            string path = DateTime.Now.ToString("yyyy-MM-dd") + "\\";
            path_tmp = path + "saves" + "\\";
            if (!Directory.Exists(path_tmp))
            {
                Directory.CreateDirectory(path_tmp);
                Console.WriteLine(path_tmp);
            }
        }

        #region Page2 Param
        //region
        Image<Bgr, byte> pg2_image_input_not_change, pg2_image_input_copy;//img1
        Image<Bgr, byte> pg2_image_compare_not_change, pg2_image_compare_copy, pg2_image_compare_show;//img2
        Color[] colors = new Color[] { Color.MistyRose, Color.LightGreen, Color.Yellow,
                                       Color.Orange, Color.MediumPurple, Color.IndianRed,
                                       Color.Red };

        double sum_dis = 0, color_threshold = 0;
        private Series[] _series1 = null;
        string txt_show = "";
        #endregion

        #region Page2

        #region open
        private void pg2_BT_open_img1_Click(object sender, EventArgs e)
        {
            try
            {
                Pg2_Func P2_Func = new Pg2_Func(this);
                string filePath = "";
                pg2_image_input_not_change = P2_Func.OpenImg(ref filePath);
                pg2_image_input_copy = pg2_image_input_not_change.Copy();
                textBox4.Text = filePath;
                label9.Text = "Size_compare:" + pg2_image_input_not_change.Width + "x" + pg2_image_input_not_change.Height;
                pg2_image_initial.Image = pg2_image_input_not_change;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void pg2_BT_open_img2_Click(object sender, EventArgs e)
        {
            try
            {
                Pg2_Func P2_Func = new Pg2_Func(this);
                string filePath = "";
                pg2_image_compare_not_change = P2_Func.OpenImg(ref filePath);
                pg2_image_compare_not_change = pg2_image_compare_not_change.Resize(pg2_image_input_not_change.Width, pg2_image_input_not_change.Height, Inter.Cubic);
                pg2_image_compare_copy = pg2_image_compare_not_change.Copy();
                textBox3.Text = filePath;
                label10.Text = "Size_compare:" + pg2_image_compare_not_change.Width + "x" + pg2_image_compare_not_change.Height;
                pg2_image_compare.Image = pg2_image_compare_not_change;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        #endregion


        private void BT_color_Click(object sender, EventArgs e)
        {
            #region para
            Image<Lab, byte> image_lab, image_lab2;
            image_lab = pg2_image_input_not_change.Copy().Convert<Lab, byte>();
            image_lab2 = pg2_image_compare_not_change.Copy().Convert<Lab, byte>();
            int height = image_lab.Height;
            int width = image_lab.Width;
            int[] color_cal = new int[] { 0, 0, 0, 0, 0, 0, 0 };
            Pg2_Func P2_Func = new Pg2_Func(this);
            double p_dis = 0;
            pg2_image_compare_not_change = P2_Func.SIFTImageAlignment(pg2_image_input_not_change.Copy(), pg2_image_compare_not_change.Copy(), pg2_image_input_not_change.Copy().Convert<Gray, byte>(), pg2_image_compare_not_change.Convert<Gray, byte>());
            pg2_image_compare_show = pg2_image_compare_not_change.Copy();
            #endregion
            for (int h = 0; h < height; h++)
            {
                for (int w = 0; w < width; w++)
                {
                    // cal dis of color
                    p_dis = P2_Func.deltaE(image_lab[h, w].X, image_lab2[h, w].X, image_lab[h, w].Y, image_lab2[h, w].Y, image_lab[h, w].Z, image_lab2[h, w].Z);
                    int i = 0;
                    if (p_dis > 0)
                    {
                        if (p_dis <= 0.5) i = 1;
                        else if (p_dis <= 1.5) i = 2;
                        else if (p_dis <= 3.0) i = 3;
                        else if (p_dis <= 6.0) i = 4;
                        else if (p_dis <= 12) i = 5;
                        else i = 6;
                    }
                    color_cal[i]++;
                    if (i > color_threshold)
                        pg2_image_compare_show[h, w] = new Bgr(colors[i]);
                    sum_dis += p_dis;
                }
            }

            #region output image
            int k = height * width;
            for (int i = 0; i < 7; i++)
            {
                color_cal[i] = (int)Math.Round((color_cal[i] / (double)(k) * 100.0));
            }
            Draw_histogram(color_cal);
            pg2_image_compare.Image = pg2_image_compare_show;
            pg2_image_compare_show.Save(DateTime.Now.ToString("yyyy-MM-dd") + $"\\saves\\fade_{DateTime.Now.ToString("HH_mm_ss")}.jpg");
            #endregion



        }

        private void trackBar_color_Scroll(object sender, EventArgs e)
        {
            string[] allx = { "無色差", "0.5up", "1.5up", "3up", "6up", "12up" };
            double[] th = { 0, 0.5, 1.5, 3, 6, 12 };
            LB_threshold.Text = "色差:" + allx[trackBar_color.Value];
            color_threshold = trackBar_color.Value;
        }

        private void Draw_histogram(int[] Y)
        {
            if (chart1.Series.Count != 0)
                chart1.Series.Clear();
            //chart2.Series["s1"].Points.Clear();
            chart1.Titles.Clear();
            chart1.Series.Add("s1");
            chart1.Titles.Add("initial image色差統計圓餅圖");
            string[] allx = { "無色差", "0~0.5", "0.5~1.5", "1.5~3", "3~6", "6~12", "12up" };
            chart1.Series["s1"].IsValueShownAsLabel = true;
            txt_show += "褪色部分 : \r\n";
            for (int i = 0; i < allx.Length; i++)
            {
                chart1.Series["s1"].ChartType = SeriesChartType.Pie;
                chart1.Series["s1"].Points.AddXY(allx[i], Y[i].ToString());
                chart1.Series["s1"].Points[i].Color = colors[i];
                txt_show += "色差 : " + allx[i].ToString() + " 占整張圖 " + Y[i].ToString() + " %\r\n";
            }
            TXT_color.AppendText(txt_show);
        }

        #endregion


    }
}