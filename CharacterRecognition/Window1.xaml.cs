using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using BPSimplified.Lib;
using System.Threading;
using BPSimplified;
using System.Drawing;
using System.IO;
using System.Collections.Specialized;
using System.Configuration;
using System.Windows.Forms;
using System.Globalization;
using QBits.Intuition.UI;

namespace CharacterRecognition
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        public Window1()
        {
            InitializeComponent();

            timer1 = new System.Windows.Forms.Timer();

            InitializeSettings();

            GenerateTrainingSet();
            CreateNeuralNetwork();

            asyCallBack = new AsyncCallback(TraningCompleted);
            ManualReset = new ManualResetEvent(false);
        }

        /// <summary>
        /// Neural network object with output type string.
        /// </summary>
        private NeuralNetwork<string> neuralNetwork = null;

        #region Data members required for neural network
        private Dictionary<string, double[]> TrainingSet = null;
        private int av_ImageHeight = 0;
        private int av_ImageWidth = 0;
        private int NumOfPatterns = 0;
        #endregion

        /// <summary>
        /// For asynchronized programming instead of handling threads.
        /// </summary>
        /// <returns></returns>
        private delegate bool TrainingCallBack();
        private AsyncCallback asyCallBack = null;
        private IAsyncResult res = null;
        private ManualResetEvent ManualReset = null;

        private DateTime DTStart;
        #region New event handlers (ported)
        private void buttonTrain_Click(object sender, RoutedEventArgs e)
        {
            UpdateState("Began Training Process..\r\n");
            SetButtons(false);
            ManualReset.Reset();

            TrainingCallBack TR = new TrainingCallBack(neuralNetwork.Train);
            res = TR.BeginInvoke(asyCallBack, TR);
            DTStart = DateTime.Now;
            timer1.Start();
        }

        #endregion

        #region Jeszcze nieprzejrzany kod
        void neuralNetwork_IterationChanged(object o, NeuralEventArgs args)
        {
            UpdateError(args.CurrentError);
            UpdateIteration(args.CurrentIteration);

            if (ManualReset.WaitOne(0, true))
                args.Stop = true;
        }

        System.Windows.Forms.Timer timer1;

        private void TraningCompleted(IAsyncResult result)
        {
            this.InvokeOnUIThread(() =>
            {
                if (result.AsyncState is TrainingCallBack)
                {
                    TrainingCallBack TR = (TrainingCallBack)result.AsyncState;

                    bool isSuccess = TR.EndInvoke(res);
                    if (isSuccess)
                        UpdateState("Completed Training Process Successfully\r\n");
                    else
                        UpdateState("Training Process is Aborted or Exceed Maximum Iteration\r\n");

                    SetButtons(true);
                    timer1.Stop();
                }
            });
        }

        private void buttonRecognize_Click(object sender, EventArgs e)
        {
            string MatchedHigh = "?", MatchedLow = "?";
            double OutputValueHight = 0, OutputValueLow = 0;

            //TODO: Complete
            double[] input = null; // ImageProcessing.ToMatrix(drawingPanel1.ImageOnPanel, av_ImageHeight, av_ImageWidth);
            neuralNetwork.Recognize(input, ref MatchedHigh, ref OutputValueHight,
                ref MatchedLow, ref OutputValueLow);

            ShowRecognitionResults(MatchedHigh, MatchedLow, OutputValueHight, OutputValueLow);

        }

        private void ShowRecognitionResults(string MatchedHigh, string MatchedLow, double OutputValueHight, double OutputValueLow)
        {
            //labelMatchedHigh.Text = " (%" + ((int)100 * OutputValueHight).ToString("##") + ")";
            highChar.Text = MatchedHigh;
            highPercent.Text = string.Format("({0:P})", ((int)100 * OutputValueHight));

            //labelMatchedLow.Text = "Low: " + MatchedLow + " (%" + ((int)100 * OutputValueLow).ToString("##") + ")";
            lowChar.Text = MatchedLow;
            lowPercent.Text = string.Format("({0:P})", ((int)100 * OutputValueLow));

            //TODO: Rewrite this
            //pictureBoxInput.Image = new Bitmap(drawingPanel1.ImageOnPanel,
            //    pictureBoxInput.Width, pictureBoxInput.Height);

            //TODO: Rewrite this
            //if (MatchedHigh != "?")
            //    pictureBoxMatchedHigh.Image = new Bitmap(new Bitmap(textBoxTrainingBrowse.Text + "\\" + MatchedHigh + ".bmp"),
            //        pictureBoxMatchedHigh.Width, pictureBoxMatchedHigh.Height);

            //if (MatchedLow != "?")
            //    pictureBoxMatchedLow.Image = new Bitmap(new Bitmap(textBoxTrainingBrowse.Text + "\\" + MatchedLow + ".bmp"),
            //        pictureBoxMatchedLow.Width, pictureBoxMatchedLow.Height);
        }

        private void buttonClear_Click(object sender, EventArgs e)
        {
            //TODO: Rewrite this
            //drawingPanel1.Clear();
        }

        private void buttonBrowse_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.OpenFileDialog FD = new System.Windows.Forms.OpenFileDialog();
            FD.Filter = "Bitmap Image(*.bmp)|*.bmp";
            FD.InitialDirectory = textBoxTrainingBrowse.Text;

            if (FD.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string FileName = FD.FileName;
                if (System.IO.Path.GetExtension(FileName) == ".bmp")
                {
                    textBoxBrowse.Text = FileName;
                    //TODO: Rewrite this
                    //drawingPanel1.ImageOnPanel = new Bitmap(new Bitmap(FileName), drawingPanel1.Width, drawingPanel1.Height);
                }
            }
            FD.Dispose();
        }

        private void buttonTrainingBrowse_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog FD = new System.Windows.Forms.FolderBrowserDialog();
            FD.SelectedPath = textBoxTrainingBrowse.Text;

            if (FD.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                textBoxTrainingBrowse.Text = FD.SelectedPath;
            }
            FD.Dispose();
        }
        private void GenerateTrainingSet()
        {
            textBoxState.AppendText("Generating Training Set..");

            string[] Patterns = System.IO.Directory.GetFiles(textBoxTrainingBrowse.Text, "*.bmp");

            TrainingSet = new Dictionary<string, double[]>(Patterns.Length);
            foreach (string s in Patterns)
            {
                Bitmap Temp = new Bitmap(s);
                TrainingSet.Add(System.IO.Path.GetFileNameWithoutExtension(s),
                    ImageProcessing.ToMatrix(Temp, av_ImageHeight, av_ImageWidth));
                Temp.Dispose();
            }

            textBoxState.AppendText("Done!\r\n");
        }

        private void buttonSaveSettings_Click(object sender, EventArgs e)
        {
            textBoxState.AppendText("Saving Settings..");

            string[] Images = Directory.GetFiles(textBoxTrainingBrowse.Text, "*.bmp");
            NumOfPatterns = Images.Length;

            av_ImageHeight = 0;
            av_ImageWidth = 0;

            foreach (string s in Images)
            {
                Bitmap Temp = new Bitmap(s);
                av_ImageHeight += Temp.Height;
                av_ImageWidth += Temp.Width;
                Temp.Dispose();
            }
            av_ImageHeight /= NumOfPatterns;
            av_ImageWidth /= NumOfPatterns;

            int networkInput = av_ImageHeight * av_ImageWidth;

            //textBoxInputUnit.Text = ((int)((double)(networkInput + NumOfPatterns) * .5)).ToString();
            //textBoxHiddenUnit.Text = ((int)((double)(networkInput + NumOfPatterns) * .3)).ToString();
            textBoxOutputUnit.Text = NumOfPatterns.ToString();


            buttonRecognize.IsEnabled = false;
            buttonSave.IsEnabled = false;

            textBoxState.AppendText("Done!\r\n");

            GenerateTrainingSet();
            CreateNeuralNetwork();
        }

        private void InitializeSettings()
        {
            textBoxState.AppendText("Initializing Settings..");

            try
            {
                NameValueCollection AppSettings = ConfigurationManager.AppSettings;

                comboBoxLayers.SelectedIndex = (Int16.Parse(AppSettings["NumOfLayers"]) - 1);
                textBoxTrainingBrowse.Text = System.IO.Path.GetFullPath(AppSettings["PatternsDirectory"]);
                textBoxMaxError.Text = AppSettings["MaxError"];

                string[] Images = Directory.GetFiles(textBoxTrainingBrowse.Text, "*.bmp");
                NumOfPatterns = Images.Length;

                av_ImageHeight = 0;
                av_ImageWidth = 0;

                foreach (string s in Images)
                {
                    Bitmap Temp = new Bitmap(s);
                    av_ImageHeight += Temp.Height;
                    av_ImageWidth += Temp.Width;
                    Temp.Dispose();
                }
                av_ImageHeight /= NumOfPatterns;
                av_ImageWidth /= NumOfPatterns;

                int networkInput = av_ImageHeight * av_ImageWidth;

                textBoxInputUnit.Text = ((int)((double)(networkInput + NumOfPatterns) * .33)).ToString();
                textBoxHiddenUnit.Text = ((int)((double)(networkInput + NumOfPatterns) * .11)).ToString();
                textBoxOutputUnit.Text = NumOfPatterns.ToString();


            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("Error Initializing Settings: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            textBoxState.AppendText("Done!\r\n");
        }

        private void CreateNeuralNetwork()
        {
            if (TrainingSet == null)
                throw new Exception("Unable to Create Neural Network As There is No Data to Train..");

            if (comboBoxLayers.SelectedIndex == 0)
            {

                neuralNetwork = new NeuralNetwork<string>
                    (new BP1Layer<string>(av_ImageHeight * av_ImageWidth, NumOfPatterns), TrainingSet);

            }
            else if (comboBoxLayers.SelectedIndex == 1)
            {
                int InputNum = Int16.Parse(textBoxInputUnit.Text);

                neuralNetwork = new NeuralNetwork<string>
                    (new BP2Layer<string>(av_ImageHeight * av_ImageWidth, InputNum, NumOfPatterns), TrainingSet);

            }
            else if (comboBoxLayers.SelectedIndex == 2)
            {
                int InputNum = Int16.Parse(textBoxInputUnit.Text);
                int HiddenNum = Int16.Parse(textBoxHiddenUnit.Text);

                neuralNetwork = new NeuralNetwork<string>
                    (new BP3Layer<string>(av_ImageHeight * av_ImageWidth, InputNum, HiddenNum, NumOfPatterns), TrainingSet);

            }

            neuralNetwork.IterationChanged +=
                new NeuralNetwork<string>.IterationChangedCallBack(neuralNetwork_IterationChanged);

            neuralNetwork.MaximumError = Double.Parse(textBoxMaxError.Text, CultureInfo.CurrentUICulture);
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            ManualReset.Set();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            TimeSpan TSElapsed = DateTime.Now.Subtract(DTStart);
            UpdateTimer(TSElapsed.Hours.ToString("D2") + ":" +
                TSElapsed.Minutes.ToString("D2") + ":" +
                TSElapsed.Seconds.ToString("D2"));
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            SaveFileDialog FD = new SaveFileDialog();
            FD.Filter = "Network File(*.net)|*.net";
            if (FD.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                neuralNetwork.SaveNetwork(FD.FileName);
            }
            FD.Dispose();
        }

        private void buttonLoad_Click(object sender, EventArgs e)
        {
            OpenFileDialog FD = new OpenFileDialog();
            FD.Filter = "Network File(*.net)|*.net";
            FD.InitialDirectory = System.Windows.Forms.Application.StartupPath;
            if (FD.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                neuralNetwork.LoadNetwork(FD.FileName);
            }
            buttonRecognize.IsEnabled = true;
            buttonSave.IsEnabled = true;

            FD.Dispose();
        }

        #region Methods To Invoke UI Components If Required
        private delegate void UpdateUI(object o);
        private void SetButtons(object o)
        {
            //if invoke is required for a control, sure, it is also required for others
            //then, it is not needed to check all controls
            //if (buttonStop.InvokeRequired)
            //{
            //    buttonStop.Invoke(new UpdateUI(SetButtons), o);
            //}
            //else
            {
                bool b = (bool)o;
                buttonStop.IsEnabled = !b;
                buttonRecognize.IsEnabled = b;
                buttonTrain.IsEnabled = b;
                buttonLoad.IsEnabled = b;
                buttonSave.IsEnabled = b;
            }
        }
        private void UpdateError(object o)
        {
            //if (labelError.InvokeRequired)
            //{
            //    labelError.Invoke(new UpdateUI(UpdateError), o);
            //}
            //else
            {
                //labelError.Text = "Error: " + ((double)o).ToString(".###");
                ErrorTB.InvokeOnUIThread(() => ErrorTB.Text = ((double)o).ToString(".###"));
            }
        }
        private void UpdateIteration(object o)
        {
            //if (labelIteration.InvokeRequired)
            //{
            //    labelIteration.Invoke(new UpdateUI(UpdateIteration), o);
            //}
            //else
            {
                //labelIteration.Text = "Iteration: " + ((int)o).ToString();
                IterationTB.InvokeOnUIThread(() => IterationTB.Text = ((int)o).ToString());
            }
        }

        private void UpdateState(object o)
        {
            //if (textBoxState.InvokeRequired)
            //{
            //    textBoxState.Invoke(new UpdateUI(UpdateState), o);
            //}
            //else
            {
                textBoxState.InvokeOnUIThread(() => textBoxState.AppendText((string)o));
            }
        }

        private void UpdateTimer(object o)
        {
            //if (TimeElapsedTB.InvokeRequired)
            //{
            //    TimeElapsedTB.Invoke(new UpdateUI(UpdateTimer), o);
            //}
            //else
            {
                TimeElapsedTB.Text = (string)o;
            }
        }

        #endregion

        #region RadioButton & CheckBox Event Handlers- Not Important
        private void radioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonBrowse.IsChecked.Value)
            {
                textBoxBrowse.IsEnabled = true;
                buttonBrowse.IsEnabled = true;
                //TODO: Rewrite this
                //drawingPanel1.IsEnabled = false;
            }
            else
            {
                textBoxBrowse.IsEnabled = false;
                buttonBrowse.IsEnabled = false;
                //TODO: Rewrite this
                //drawingPanel1.IsEnabled = true;
            }
        }

        private void comboBoxLayers_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBoxLayers.SelectedIndex == 0)
            {
                textBoxInputUnit.IsEnabled = false;
                textBoxHiddenUnit.IsEnabled = false;
            }
            else if (comboBoxLayers.SelectedIndex == 1)
            {
                textBoxInputUnit.IsEnabled = true;
                textBoxHiddenUnit.IsEnabled = false;
            }
            else if (comboBoxLayers.SelectedIndex == 2)
            {
                textBoxInputUnit.IsEnabled = true;
                textBoxHiddenUnit.IsEnabled = true;
            }
        }
        #endregion

        #endregion
    }
}
