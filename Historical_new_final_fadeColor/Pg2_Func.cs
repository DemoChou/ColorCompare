using Emgu.CV.Structure;
using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV.CvEnum;
using Emgu.CV.Features2D;
using Emgu.CV.Util;

namespace Historical_new_final
{
    internal class Pg2_Func
    {
        private Form1 form1;
        public Pg2_Func(Form1 form)
        {
            this.form1 = form;
        }
        /* main */
        public Image<Bgr, byte> OpenImg(ref string filePath)
        {
            Image<Bgr, byte> image = null;
            try
            {
                OpenFileDialog ofd = new OpenFileDialog();
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    filePath = ofd.FileName;
                    image = new Image<Bgr, byte>(filePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            return image;
        }

        public Image<Bgr, byte> SIFTImageAlignment(Image<Bgr, byte> image1, Image<Bgr, byte> image2, Image<Gray, byte> grayImage1, Image<Gray, byte> grayImage2)
        {
            // 初始化SIFT檢測器
            SIFT sift = new SIFT();
            // 創建KeyPoint列表以保存檢測到的特徵點
            VectorOfKeyPoint keypoints1 = new VectorOfKeyPoint();
            VectorOfKeyPoint keypoints2 = new VectorOfKeyPoint();

            // 檢測特徵點並計算描述子
            Matrix<float> descriptors1 = new Matrix<float>(1024, 1024);
            Matrix<float> descriptors2 = new Matrix<float>(1024, 1024);

            sift.DetectAndCompute(grayImage1, null, keypoints1, descriptors1, false);
            sift.DetectAndCompute(grayImage2, null, keypoints2, descriptors2, false);

            // 創建Brute-Force匹配器
            BFMatcher bf = new BFMatcher(DistanceType.L2);

            // 使用knn匹配算法找到最佳匹配
            VectorOfVectorOfDMatch matches = new VectorOfVectorOfDMatch();
            bf.KnnMatch(descriptors1, descriptors2, matches, 2);

            // 根據比值測試保留好的匹配
            List<MDMatch> goodMatches = new List<MDMatch>();
            for (int i = 0; i < matches.Size; i++)
            {
                if (matches[i][0].Distance < 0.75 * matches[i][1].Distance)
                {
                    goodMatches.Add(matches[i][0]);
                }
            }

            // 提取匹配點的位置
            PointF[] points1 = goodMatches.Select(m => keypoints1[m.QueryIdx].Point).ToArray();
            PointF[] points2 = goodMatches.Select(m => keypoints2[m.TrainIdx].Point).ToArray();

            // 使用RANSAC算法估計透視變換矩陣
            Mat homography = CvInvoke.FindHomography(points1, points2, RobustEstimationAlgorithm.Ransac);

            // 對第2個影像進行透視變換
            Image<Bgr, byte> alignedImage1 = image2;
            CvInvoke.WarpPerspective(image2, alignedImage1, homography, image1.Size);
            
            return alignedImage1;
        }

        public double deltaE(double x1, double x2, double y1, double y2, double z1, double z2)
        {
            double E = 0;
            double[] LAB1 = new double[3];
            double[] LAB2 = new double[3];
            LAB1 = xyzToLab(x1, y1, z1);
            LAB2 = xyzToLab(x2, y2, z2);
            E = Math.Sqrt(Math.Pow(Math.Abs(LAB1[0] - LAB2[0]), 2) + Math.Pow(Math.Abs(LAB1[1] - LAB2[1]), 2) * Math.Pow(Math.Abs(LAB1[2] - LAB2[2]), 2));//
            return E;
        }



        #region function
        private double[] xyzToLab(double x, double y, double z)
        {
            double Xn = 0.9515, Yn = 1, Zn = 1.0886;//轉換參數
            double[] LAB = new double[3];
            if (y / Yn > 0.008856)
                LAB[0] = (116 * Math.Pow(y / Yn, 1.0 / 3.0)) - 16;
            else
                LAB[0] = 903.3 * y / Yn;
            LAB[1] = 500 - (f(x / Xn) - f(y / Yn));
            LAB[2] = 200 * (f(y / Yn) - f(z / Zn));
            return LAB;
        }
        private double f(double t)
        {
            double result = 0;
            if (t > 0.008856)
                result = Math.Pow(t, 1 / 3.0);
            else
                result = 7.787 * t + 16 / 116.0;
            return result;
        }
        #endregion
    }
}
