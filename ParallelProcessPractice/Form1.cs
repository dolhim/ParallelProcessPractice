using BitmapChipMaker;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ParallelProcessPractice
{
    public partial class Form1 : Form
    {
        // split 개수
        int count = 4;

        // 입력 영상
        string path;
        int bmpW, bmpH;

        // 비동기 처리시 사용될 배열
        byte[] target;

        public Form1()
        {
            InitializeComponent();
        }

        #region -- Control
        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                textBox1.AppendText("\r\nParallel ");
                Run(true, dialog.FileName);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                textBox1.AppendText("\r\nSingle thread ");
                Run(false, dialog.FileName);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            SaveArray2Image();
        }
        #endregion

        // 입력 영상 정보를 설정함
        private void SetImage(string path)
        {
            this.path = path;
            using (Bitmap bmp = new Bitmap(path))
            {
                this.bmpW = bmp.Width;
                this.bmpH = bmp.Height;
            }

            this.target = new byte[bmpH * bmpW * 3];
            textBox1.AppendText(string.Format("---------------\r\n" +
                                              "파일명 : {0} \r\n" +
                                              "크기 : ({1}, {2})px \r\n" +
                                              "---------------\r\n",
                                              Path.GetFileName(path), bmpW, bmpH));
        }

        // 비동기 배열에 저장된 정보를 파일로 저장
        private void SaveArray2Image()
        {
            // 결과 파일로 저장
            var dstPath = Path.Combine(Path.GetDirectoryName(path),
                         Path.GetFileNameWithoutExtension(path) +
                         "_all" + Path.GetExtension(path));
            var dstRGB = BitmapBuffer.GetBitmapRGB(target, bmpW, bmpH);
            dstRGB.Save(dstPath, System.Drawing.Imaging.ImageFormat.Tiff);
            dstRGB.Dispose();
        }

        private void Run(bool parallel, string path)
        {
            SetImage(path);

            Stopwatch sw = Stopwatch.StartNew();


            for (int i = 0; i < count; ++i)
            {
                string src = path;
                string dst = Path.Combine(Path.GetDirectoryName(src),
                         Path.GetFileNameWithoutExtension(src) +
                         "_cut" + i + Path.GetExtension(src));

                Point pt = new Point(0, i * bmpH / count);
                int sizeW = bmpW;
                int sizeH = bmpH / count;
                Rectangle rect = new Rectangle(pt, new Size(sizeW, sizeH));

                var result = Split(i, src, bmpW, bmpH, target, dst, rect, parallel);
            }

            if (!parallel)
            {
                textBox1.AppendText("total : " + sw.ElapsedMilliseconds + "ms\r\n");
                textBox1.ScrollToCaret();
            }
        }

        private async Task<bool> Split(int n, string src, int srcW, int srcH,
                                      byte[] dstArr, string dst, Rectangle rect, bool parallel)
        {
            bool ret = false;
            Stopwatch sw = Stopwatch.StartNew();

            if (parallel)
            {
                ret = await SplitImageAsync(n, src, srcW, srcH, dstArr, dst, rect);
            }
            else
            {
                ret = SplitImage(n, src, srcW, srcH, dstArr, dst, rect);
            }
            sw.Stop();

            textBox1.AppendText("work " + n + " - " + sw.ElapsedMilliseconds + "ms\r\n");
            textBox1.ScrollToCaret();

            return ret;
        }

        private async Task<bool> SplitImageAsync(int n, string src, int srcW, int srcH,
                                         byte[] dstArr, string dst, Rectangle rect)
        {
            CancellationTokenSource cts = new CancellationTokenSource();

            bool runResult = await Task.Run(() => 
                BitmapChipMaker.ChipMaker.SaveChipRGB(src, srcW, srcH, dstArr,
                dst, rect.X, rect.Y, rect.Width, rect.Height), cts.Token);

            return runResult;
        }

        private bool SplitImage(int n, string src, int srcW, int srcH,
                        byte[] dstArr, string dst, Rectangle rect)
        {
            BitmapChipMaker.ChipMaker.SaveChipRGB(src, srcW, srcH, dstArr,
                dst, rect.X, rect.Y, rect.Width, rect.Height);
            
            return true;
        }
    }
}

