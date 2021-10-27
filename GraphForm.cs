using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Collections.ObjectModel;
using CrossThreadLib;

namespace PowerSupply
{
    public partial class GraphForm : Form
    {
        public GraphForm()
        {
            InitializeComponent();
        }
        public Collection<double> timeCollection = new Collection<double>();
        public Collection<double> voltageCollection = new Collection<double>();
        public Collection<double> currentCollection = new Collection<double>();

        public int numberOfPoints;
        public bool isReceivingData;



        public void PlotAppend(double elapsedTime, double voltage, double current)
        {
            timeCollection.Add(elapsedTime);
            voltageCollection.Add(voltage);
            currentCollection.Add(current);
            if (chart1.InvokeRequired)
            {
                this.Invoke((System.Action)(() =>
                {
                    chart1.Series[0].Points.AddXY(elapsedTime, voltage);
                    chart1.Series[1].Points.AddXY(elapsedTime, current);
                }));
            }
            else
            {
                chart1.Series[0].Points.AddXY(elapsedTime, voltage);
                chart1.Series[1].Points.AddXY(elapsedTime, current);
            }
        }
        
        


        //public void PlotSpectrum2(double[] xValue, double[] yValue, int numberOfPointsToPlot)
        //{
        //    if (scatterGraph1.InvokeRequired)
        //    {
        //        this.Invoke((System.Action)(() =>
        //        {
        //           //scatterGraph1.ClearData();
        //            scatterGraph1.PlotXYAppend(xValue, yValue, 0, numberOfPointsToPlot);                   
        //        }));
        //    }
        //    else
        //    {
        //      //  scatterGraph1.ClearData();
        //        scatterGraph1.PlotXYAppend(xValue, yValue);
        //    }
        //}

        


        private void changePlotColorStripButton_Click(object sender, EventArgs e)
        {
            Action changeColor = new Action(ChangePlotColorVoltage);
            changeColor.BeginInvoke(null, null);
        }

        private void ChangePlotColorVoltage()
        {
            colorDialog1.ShowDialog();

            if (chart1.InvokeRequired)
            {
                this.Invoke((System.Action)(() =>
                {
                    chart1.Series[0].Color = colorDialog1.Color;
                    chart1.ChartAreas[0].AxisY.TitleForeColor= colorDialog1.Color;

                }));
            }
            else
            {
                chart1.Series[0].Color = colorDialog1.Color;
                chart1.ChartAreas[0].AxisY.TitleForeColor = colorDialog1.Color;
            }
        }

        private void ChangePlotColorCurrent()
        {
            colorDialog1.ShowDialog();

            if (chart1.InvokeRequired)
            {
                this.Invoke((System.Action)(() =>
                {
                    chart1.Series[1].Color = colorDialog1.Color;
                    chart1.ChartAreas[0].AxisY2.TitleForeColor = colorDialog1.Color;

                }));
            }
            else
            {
                chart1.Series[1].Color = colorDialog1.Color;
                chart1.ChartAreas[0].AxisY2.TitleForeColor = colorDialog1.Color;
            }
        }

        private void saveFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            try
            {
                string fileName;
                fileName = saveFileDialog1.FileName;
                System.IO.File.WriteAllLines(fileName, combinedValuesToSave);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            
        }

        private Collection<string> combinedValuesToSave = new Collection<string>();

        private void saveSpectrumToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool isOperationSuccessful;
            string errorMessage;
            SaveValues(out isOperationSuccessful, out errorMessage);
            if (!isOperationSuccessful)
            {
                return;
            }
           
        }

        private void SaveValues(out bool isOperationSuccessful, out string errorMessage)
        {
            try
            {
                combinedValuesToSave.Clear();

                string firstColumn = "Elapsed Time [min]";
                string secondColumn = "Voltage [V]";
                string thirdColumn = "Current [A]";
                combinedValuesToSave.Add(String.Format("{0}\t{1}\t{2}", firstColumn, secondColumn, thirdColumn));
                for (int i = 0; i < numberOfPoints; i++)
                {
                    firstColumn = String.Format("{0:0.000000}", timeCollection[i]);
                    secondColumn = String.Format("{0:0.000000}", voltageCollection[i]);
                    thirdColumn = String.Format("{0:0.000000}", currentCollection[i]);
                    combinedValuesToSave.Add(String.Format("{0}\t{1}\t{2}", firstColumn, secondColumn,thirdColumn));
                }
                saveFileDialog1.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
                saveFileDialog1.ShowDialog();
                isOperationSuccessful = true;
                errorMessage = "";
            }
            catch (Exception ex)
            {
                isOperationSuccessful = false;
                errorMessage = String.Format("{0}", ex);
            }
        }

        private void chart1_MouseMove(object sender, MouseEventArgs e)
        {
            double xPosition = chart1.ChartAreas[0].CursorX.Position;
            double yPosition = chart1.ChartAreas[0].CursorY.Position;
            xPositionTextBox.Text = String.Format("{0:0.0}", xPosition);
            yPositionTextBox.Text = String.Format("{0:0.00000}", yPosition);
        }

        private void GraphForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (isReceivingData)
            {
                Action displayMessageBox = new Action(DisplayMessageBox);
                displayMessageBox.BeginInvoke(null, null);                
                e.Cancel = true;
            }
            else
            {
                e.Cancel = false;
            }

        }

        public void SetFormTitle(string formTitle)
        {
            if (this.InvokeRequired)
            {
                this.Invoke((System.Action)(() =>
                {
                    this.Text = formTitle;
                }));
            }
            else
            {
                this.Text = formTitle;
            }
        }
        private void DisplayMessageBox()
        {
            MessageBox.Show("DAQ in progress!\nPlease, stop acquisition before closing this window.", "Exit cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void GraphForm_Load(object sender, EventArgs e)
        {
            chart1.ChartAreas[0].AxisX.LabelStyle.Format = "0.00";
            chart1.ChartAreas[0].AxisY.LabelStyle.Format = "0.00";
            chart1.ChartAreas[0].AxisY.MajorTickMark.Interval = 0.1;
            chart1.ChartAreas[0].AxisY.MinorTickMark.Interval = 0;
            chart1.ChartAreas[0].AxisY2.LabelStyle.Format = "0.000";
            chart1.ChartAreas[0].AxisY2.MajorGrid.Enabled = false;
            chart1.ChartAreas[0].AxisX.MinorGrid.Enabled = false;
            chart1.ChartAreas[0].AxisY.MinorGrid.Enabled = false;
            chart1.ChartAreas[0].AxisY2.MinorGrid.Enabled = false;
            chart1.ChartAreas[0].AxisX.MajorGrid.Enabled = false;
            chart1.ChartAreas[0].AxisY.MajorGrid.Enabled = false;

        }

        public void SetXAxisLimits(double minimum)
        {
            if (chart1.InvokeRequired)
            {
                this.Invoke((System.Action)(() =>
                {
                    chart1.ChartAreas[0].AxisX.Minimum = minimum;         
                }));
            }
            else
            {
                chart1.ChartAreas[0].AxisX.Minimum = minimum;         
            }

                  
        }


        public void SetYAxisTitle(string axisTitle)
        {
            chart1.ChartAreas[0].AxisY.Title = axisTitle; 
        }

        public static int Clamp(int value, int min, int max)
        {
            return (value < min) ? min : (value > max) ? max : value;
        }

        
        private int lineWidth = 1;
        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            Action increaseLineWidth = new Action(IncreaseLineWidth);
            increaseLineWidth.BeginInvoke(null, null);           
        }

        private void IncreaseLineWidth()
        {
            lineWidth = Clamp(lineWidth +1,1,10);  
            
            if (chart1.InvokeRequired)
            {
                this.Invoke((System.Action)(() =>
                {                    
                    chart1.Series[0].BorderWidth = lineWidth;  
                }));
            }
            else
            {
                chart1.Series[0].BorderWidth = lineWidth;  
                lineWidth++;
            }
            
        }

        private void DecreaseLineWidth()
        {
            lineWidth = Clamp(lineWidth - 1, 1, 10);

            if (chart1.InvokeRequired)
            {
                this.Invoke((System.Action)(() =>
                {
                    chart1.Series[0].BorderWidth = lineWidth;  
                }));
            }
            else
            {
                chart1.Series[0].BorderWidth = lineWidth;             
            }

        }

        private void IncreaseLineWidthCurrent()
        {
            lineWidth = Clamp(lineWidth + 1, 1, 10);

            if (chart1.InvokeRequired)
            {
                this.Invoke((System.Action)(() =>
                {
                    chart1.Series[1].BorderWidth = lineWidth;
                }));
            }
            else
            {
                chart1.Series[1].BorderWidth = lineWidth;
                lineWidth++;
            }

        }

        private void DecreaseLineWidthCurrent()
        {
            lineWidth = Clamp(lineWidth - 1, 1, 10);

            if (chart1.InvokeRequired)
            {
                this.Invoke((System.Action)(() =>
                {
                    chart1.Series[1].BorderWidth = lineWidth;
                }));
            }
            else
            {
                chart1.Series[1].BorderWidth = lineWidth;
            }

        }


        private void toolStripButton2_Click(object sender, EventArgs e)
        {

            Action decreaseLineWidth = new Action(DecreaseLineWidth);
            decreaseLineWidth.BeginInvoke(null, null);   
        }

        private void fileToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        bool isZoomEnabled = false;

        private void zoomButton_Click(object sender, EventArgs e)
        {
            if (!isZoomEnabled)
            {
                chart1.ChartAreas[0].CursorX.IsUserSelectionEnabled = true;
                chart1.ChartAreas[0].CursorY.IsUserSelectionEnabled = true;
                isZoomEnabled = true;
                chart1.Cursor = Cursors.Hand;
                zoomButton.Checked = true;
            }
            else
            {
                chart1.ChartAreas[0].CursorX.IsUserSelectionEnabled = false;
                chart1.ChartAreas[0].CursorY.IsUserSelectionEnabled = false;
                isZoomEnabled = false;
                chart1.Cursor = Cursors.Cross;
                zoomButton.Checked = false;
            }        
        }

        private void saveSpectrumButton_Click(object sender, EventArgs e)
        {
            bool isOperationSuccessful;
            string errorMessage;
            numberOfPoints = timeCollection.Count;
            SaveValues(out isOperationSuccessful, out errorMessage);
            if (!isOperationSuccessful)
            {
                return;
            }
           
        }

        private void loadSpectrumToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.ShowDialog();  
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            try
            {
                string fileName;
                fileName = openFileDialog1.FileName;
                string[] lines = System.IO.File.ReadAllLines(fileName);
                int numberOfLines = lines.Count();
                double[] wavelengthsSavedData = new double[numberOfLines];
                double[] intensitiesSavedData = new double[numberOfLines];
                char[] delimiterChars = { '\t' };
                int i = 0;
                foreach (string line in lines)
                {
                    string[] oneLine = line.Split(delimiterChars);
                    wavelengthsSavedData[i] = Convert.ToDouble(oneLine[0]);
                    intensitiesSavedData[i] = Convert.ToDouble(oneLine[1]);
                    i++;
                }
                PlotSavedSpectrum(wavelengthsSavedData, intensitiesSavedData, numberOfLines);
            }
            catch (Exception ex)
            {
                MessageBox.Show("It is not possible to open the selected file! \n" + ex.Message, "Error opening file", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void PlotSavedSpectrum(double[] xValue, double[] yValue, int numberOfPointsToPlot)
        {
            if (chart1.InvokeRequired)
            {
                this.Invoke((System.Action)(() =>
                {
                    chart1.Series["SavedSpectrum"].Points.Clear();

                    for (int i = 0; i < numberOfPointsToPlot; i++)
                    {
                        chart1.Series["SavedSpectrum"].Points.AddXY(xValue[i], yValue[i]);
                    }
                    //time = xValue;
                    //voltage = yValue;
                    numberOfPoints = numberOfPointsToPlot;
                }));
            }
            else
            {
                chart1.Series["SavedSpectrum"].Points.Clear();

                for (int i = 0; i < numberOfPointsToPlot; i++)
                {
                    chart1.Series["SavedSpectrum"].Points.AddXY(xValue[i], yValue[i]);
                }
                //time = xValue;
                //voltage = yValue;
                numberOfPoints = numberOfPointsToPlot;
            }
        }

        private void clearLoadedSpectrumToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (chart1.InvokeRequired)
            {
                this.Invoke((System.Action)(() =>
                {
                    chart1.Series["SavedSpectrum"].Points.Clear();

                }));
            }
            else
            {
                chart1.Series["SavedSpectrum"].Points.Clear();
            }
        }

        private void chart1_Click(object sender, EventArgs e)
        {

        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            Action changeColor = new Action(ChangePlotColorCurrent);
            changeColor.BeginInvoke(null, null);
        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            Action increaseLineWidth = new Action(IncreaseLineWidthCurrent);
            increaseLineWidth.BeginInvoke(null, null);
        }

        private void toolStripButton5_Click(object sender, EventArgs e)
        {
            Action decreaseLineWidth = new Action(DecreaseLineWidthCurrent);
            decreaseLineWidth.BeginInvoke(null, null);   
        }

        private void toolStripButton6_Click(object sender, EventArgs e)
        {
            Action changeColor = new Action(ChangePlotColorCurrent);
            changeColor.BeginInvoke(null, null);
        }
    }
}