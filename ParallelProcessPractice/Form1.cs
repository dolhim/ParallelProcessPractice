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
        #endregion

        private void Run(bool parallel, string path)
        {
            int bmpW = 0; int bmpH = 0;
            using (Bitmap bmp = new Bitmap(path))
            {
                bmpW = bmp.Width;
                bmpH = bmp.Height;
            }

            textBox1.AppendText(string.Format("---------------\r\n" + 
                                              "파일명 : {0} \r\n" +
                                              "크기 : ({1}, {2})px \r\n" + 
                                              "---------------\r\n", 
                                              Path.GetFileName(path), bmpW, bmpH));

            Stopwatch sw = Stopwatch.StartNew();

            for (int i = 0; i < count; ++i)
            {
                string src = path;
                string dst = Path.Combine(Path.GetDirectoryName(src),
                         Path.GetFileNameWithoutExtension(src) +
                         "_cut" + i + Path.GetExtension(src));

                Point pt = new Point(0, i * bmpH / 4);
                int sizeW = bmpW;
                int sizeH = bmpH / 4;
                Rectangle rect = new Rectangle(pt, new Size(sizeW, sizeH));

                var result = Split(i, src, bmpW, bmpH, dst, rect, parallel);
            }

            if (!parallel)
            {
                textBox1.AppendText("total : " + sw.ElapsedMilliseconds + "ms\r\n");
                textBox1.ScrollToCaret();
            }
        }

        private async Task<bool> Split(int n, string src, int srcW, int srcH,
                                      string dst, Rectangle rect, bool parallel)
        {
            bool ret = false;
            Stopwatch sw = Stopwatch.StartNew();

            if (parallel)
            {
                ret = await SplitImageAsync(n, src, srcW, srcH, dst, rect);
            }
            else
            {
                ret = SplitImage(n, src, srcW, srcH, dst, rect);
            }
            sw.Stop();

            textBox1.AppendText("work " + n + " - " + sw.ElapsedMilliseconds + "ms\r\n");
            textBox1.ScrollToCaret();

            return ret;
        }

        private async Task<bool> SplitImageAsync(int n, string src, int srcW, int srcH,
                                                        string dst, Rectangle rect)
        {
            CancellationTokenSource cts = new CancellationTokenSource();

            bool runResult = await Task.Run(() => 
                BitmapChipMaker.ChipMaker.SaveChipRGB(src, srcW, srcH, 
                dst, rect.X, rect.Y, rect.Width, rect.Height), 
                cts.Token);

            return runResult;
        }

        private bool SplitImage(int n, string src, int srcW, int srcH,
                                       string dst, Rectangle rect)
        {
            BitmapChipMaker.ChipMaker.SaveChipRGB(src, srcW, srcH, 
                dst, rect.X, rect.Y, rect.Width, rect.Height);
            
            return true;
        }
    }
}

