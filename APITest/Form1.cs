using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Net.Http;
using System.IO;
using Newtonsoft.Json;
using DIPLi;

namespace OCRAPITest
{
    public partial class Form1 : Form
    {

        public string ImagePath { get; set; }
        public string PdfPath { get; set; }
        public Imagem I;

        public Form1()
        {
            InitializeComponent();
            cmbLanguage.SelectedIndex = 6;
        }

        private string getSelectedLanguage()
        {

            string strLang = "";
            switch (cmbLanguage.SelectedIndex)
            {
                case 0:
                    strLang = "ara";
                    break;

                case 1:
                    strLang = "chs";
                    break;

                case 2:
                    strLang = "cht";
                    break;
                case 3:
                    strLang = "cze";
                    break;
                case 4:
                    strLang = "dan";
                    break;
                case 5:
                    strLang = "dut";
                    break;
                case 6:
                    strLang = "eng";
                    break;
                case 7:
                    strLang = "fin";
                    break;
                case 8:
                    strLang = "fre";
                    break;
                case 9:
                    strLang = "ger";
                    break;
                case 10:
                    strLang = "gre";
                    break;
                case 11:
                    strLang = "hun";
                    break;
                case 12:
                    strLang = "jap";
                    break;
                case 13:
                    strLang = "kor";
                    break;
                case 14:
                    strLang = "nor";
                    break;
                case 15:
                    strLang = "pol";
                    break;
                case 16:
                    strLang = "por";
                    break;
                case 17:
                    strLang = "spa";
                    break;
                case 18:
                    strLang = "swe";
                    break;
                case 19:
                    strLang = "tur";
                    break;

            }
            return strLang;

        }

       private void button1_Click(object sender, EventArgs e)
        {
            PdfPath = ImagePath = ""; pictureBox1.BackgroundImage = null;
            OpenFileDialog fileDlg = new OpenFileDialog();
            fileDlg.Filter = "jpeg files|*.jpg;*.JPG";
            if (fileDlg.ShowDialog() == DialogResult.OK)
            {
                FileInfo fileInfo = new FileInfo(fileDlg.FileName);
                if (fileInfo.Length > 5* 1024 * 1024)
                {
                    MessageBox.Show("Limite de tamanho de arquivo de imagem atingido (1MB free API)");
                    return;
                }
                pictureBox1.Image = Image.FromFile(fileDlg.FileName);
                ImagePath = fileDlg.FileName;
                lblInfo.Text = "Imagem carregada: "+ fileInfo.Name;
                lblInfo.BackColor = Color.LightGreen;
            }

            I = new Imagem(ImagePath);
        }

        public Imagem Convolucao(Imagem I, int[,] W)
        {
            Imagem R = new Imagem(I.Largura, I.Altura);

            for (int i = 2; i < I.Altura - 2; i++)
            {
                for (int j = 2; j < I.Largura - 2; j++)
                {
                    R[i, j] = (W[0, 0] * I[i - 2, j - 2] + W[0, 1] * I[i - 1, j - 2] + W[0, 2] * I[i, j - 2] + W[1, 0] * I[i + 1, j - 2] + W[1, 1] * I[i + 2, j - 2] +
                               W[1, 2] * I[i - 2, j - 1] + W[2, 0] * I[i - 1, j - 1] + W[2, 1] * I[i, j - 1] + W[2, 2] * I[i + 1, j - 1]) / 9;
                }
            }
            return R;

        }

        private async void button2_Click(object sender, EventArgs e)
        {

            I = I.ToGrayscale();

            int[,] W = new int[,] { { 1, 1, 1 },
                                    { 1, 1, 1 },
                                    { 1, 1, 1 } };

            int[,] W2 = new int[,] { { -1, -1, -1 },
                                    { -1, 8, -1 },
                                    { -1, -1, -1 } };

            Imagem R = Convolucao(I, W);

            pictureBox2.Image = R.ToBitmap();

            //Método de Limiarização Global
            for (int i = 0; i < R.Altura; i++)
            {
                for (int j = 0; j < R.Largura; j++)
                {
                    if (R[i, j] <= 115)
                    {
                        R[i, j] = 0;
                    }
                    else
                    {
                        R[i, j] = 255;
                    }
                }
            }

            pictureBox5.Image = R.ToBitmap();
            R.Salvar("converter.bmp");

            Imagem Y = Convolucao(R, W2);
            pictureBox4.Image = Y.ToBitmap();

            for (int i = 0; i < R.Altura; i++)
            {
                for (int j = 0; j < R.Largura; j++)
                {
                    if (R[i, j] == 255)
                    {
                        R[i, j] = 0;
                    }
                    else
                    {
                        R[i, j] = I[i, j];
                    }
                }
            }
            pictureBox3.Image = R.ToBitmap();


            if (string.IsNullOrEmpty(ImagePath))
                return;

            txtResult.Text = "";
            button1.Enabled = false;
            button2.Enabled = false;
            cmbLanguage.Enabled = false;
            label2.Visible = true;

            try
            {
                HttpClient httpClient = new HttpClient();
                httpClient.Timeout = new TimeSpan(1, 1, 1);

                MultipartFormDataContent form = new MultipartFormDataContent();
                form.Add(new StringContent("2"), "OCREngine");
                form.Add(new StringContent("f8db964da588957"), "apikey");
                form.Add(new StringContent(getSelectedLanguage()), "language");

                ImagePath = "converter.bmp";
                if (string.IsNullOrEmpty(ImagePath) == false)
                {
                    byte[] imageData = File.ReadAllBytes(ImagePath);
                    form.Add(new ByteArrayContent(imageData, 0, imageData.Length), "image", "image.jpg");
                }

                HttpResponseMessage response = await httpClient.PostAsync("https://api.ocr.space/Parse/Image", form);

                string strContent = await response.Content.ReadAsStringAsync();

                Rootobject ocrResult = JsonConvert.DeserializeObject<Rootobject>(strContent);
  
                if (ocrResult.OCRExitCode == 1)
                  {
                         for (int i = 0; i < ocrResult.ParsedResults.Count() ; i++)
                         {
                             txtResult.Text = txtResult.Text + ocrResult.ParsedResults[i].ParsedText ;
                         }
                     }
                     else
                     {
                         MessageBox.Show("ERRO: " + strContent);
                     }
                    
            }
            catch (Exception)
            {
                MessageBox.Show("Algo deu errado!");
            }

            label2.ForeColor = Color.DarkGreen;
            label2.Text = "SUCESSO";
            txtResult.Enabled = true;
            button1.Enabled = true;
            button2.Enabled = true;
            cmbLanguage.Enabled = true;

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}



