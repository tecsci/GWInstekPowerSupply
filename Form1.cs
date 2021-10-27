using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using PowerSupplyDriver;
using CrossThreadLib;
using System.Resources;
using System.Diagnostics;




namespace PowerSupply
{
    public partial class Form1 : Form
    {        
        bool isPowerSupplyConnected = false;
        bool statusThread = false;
        bool readValues = false;
        private Task daqThread;
        Task task;
        private bool isLimitCurrentEnabled = false;
        private double limitCurrent = 0;
        private double totalRunTime = 0;
        private int timeInterval = 100;
        private double setVoltageConstant = 0;
        private double setMaxCurrentConstant = 0;

        private double minVoltageSine = 0;
        private double maxVoltageSine = 0;
        private double fVoltageSine = 0;
        private double setMaxCurrentSine = 0;

        private double minVoltageTriangular = 0;
        private double maxVoltageTriangular = 0;
        private double fVoltageTriangular = 0;
        private double setMaxCurrentTriangular = 0;

        private double minVoltageRamp = 0;
        private double maxVoltageRamp = 0;
        private double t1Ramp = 0;
        private double t2Ramp = 0;
        private double t3Ramp = 0;
        private double setMaxCurrentRamp = 0;




        public Form1()
        {
            InitializeComponent();
        }

        private PowerSupplyDriver.PowerSupplyDriver powerSupply;
        private GraphForm graphForm;
        private CancellationTokenSource cancellationTokenSource;

        private void Form1_Load(object sender, EventArgs e)
        {
            toolStripComboBoxPortNames.ComboBox.Items.Clear();
            toolStripComboBoxPortNames.ComboBox.DataSource = SerialPort.GetPortNames();
            toggleButton.BackgroundImage= PowerSupply_WFM.Properties.Resources.toogle_off;
            groupBoxDisplay.Enabled = false;
            groupBoxSettings.Enabled = false;
            groupBoxProgram.Enabled = false;
        }

        private void toggleButton_CheckedChanged(object sender, EventArgs e)
        {
            bool isOperationSuccesful;
            string errorMessage;

            if (toggleButton.Checked)
            {
                toggleButton.BackgroundImage = PowerSupply_WFM.Properties.Resources.toogle_on;
                powerSupply.SetOutputModeON(out isOperationSuccesful, out errorMessage);
            }
            else 
            {
                toggleButton.BackgroundImage = PowerSupply_WFM.Properties.Resources.toogle_off;
                powerSupply.SetOutputModeOFF(out  isOperationSuccesful, out  errorMessage);
            }

            if (!isOperationSuccesful)
            {
                MessageBox.Show("Problem changing output mode ON/OFF. \n" + errorMessage, "Problem detected",
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                return;
            }
        }

        private void toolStripButtonConnect_Click(object sender, EventArgs e)
        {
            bool isOperationSuccesful;
            string errorMessage;
            
            string portName = toolStripComboBoxPortNames.ComboBox.SelectedItem.ToString();            

            if (!isPowerSupplyConnected)
            {
                toolStripStatusLabel1.Text = "Connecting to port " + portName + "...";
                powerSupply = new PowerSupplyDriver.PowerSupplyDriver(portName);

                powerSupply.connectToPowerSupply(out isOperationSuccesful, out errorMessage);
                if (isOperationSuccesful && powerSupply.modelName != "")
                {
                    isPowerSupplyConnected = true;
                    toolStripStatusLabel1.Text = "Status: Connected to " + powerSupply.modelName + " through port " + portName;
                    EnableComponentsBtnConnect(true);                    
                    int outputMode = powerSupply.ReadOutputMode(out isOperationSuccesful, out errorMessage);
                    if (!isOperationSuccesful)
                    {
                        MessageBox.Show("Problem sending/recieving data . \n" + errorMessage, "Problem 002",
                            MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                        return;
                    }

                    if (outputMode == 0)
                    {
                        toggleButton.Checked = false;
                    }
                    else if (outputMode == 1)
                    {
                        toggleButton.Checked = true;
                    }
                    powerSupply.SetVoltageCurrent(0.0, 0.0, out isOperationSuccesful, out errorMessage);
                    if (!isOperationSuccesful)
                    {
                        MessageBox.Show("Problem sending/recieving data . \n" + errorMessage, "Problem 003",
                            MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                        return;
                    }
                    statusThread = true;
                    readValues = true;
                    task = new Task(ReadValues);
                    task.Start();
                }
                else
                {
                    MessageBox.Show("Problem connecting to power supply. \n" + errorMessage, "Problem 001",
                        MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                    toolStripStatusLabel1.Text = powerSupply.exceptionMessage;
                    isPowerSupplyConnected = false;
                    return;
                }
            }
            else
            {
                return;
            }
        }
        private void EnableComponentsBtnConnect(bool enableOrDissable)
        {
            bool isOperationSuccesful;
            string errorMessage;

            CrossThread.EnableControl(groupBoxDisplay, enableOrDissable);
            CrossThread.EnableControl(groupBoxProgram, enableOrDissable);
            CrossThread.EnableControl(groupBoxSettings, enableOrDissable);

            if (!enableOrDissable)
            {
                powerSupply = null;                
                isPowerSupplyConnected = false;
                readValues = false;
                CrossThread.SetTextToolStripLabel(statusStrip1, toolStripStatusLabel1, "Not connected");               
            }

        }
        private void ReadValues()
        {
            bool isOperationSuccesful;
            string errorMessage;

            while (statusThread)
            {
                if (readValues)
                {
                    try {
                        //isPowerSupplyConnected = powerSupply.IsConnected(out errorMessage);

                        //if (!isPowerSupplyConnected)
                        //{                           
                         //   EnableComponentsBtnConnect(false);
                         //   CrossThread.SetTextToolStripLabel(statusStrip1, toolStripStatusLabel1, "Error 001. Power supply not connected. " + errorMessage);
                         //   return;
                        //}

                        double voltage = powerSupply.ReadVoltage(out isOperationSuccesful, out errorMessage);
                        if (!isOperationSuccesful)
                        {                            
                            EnableComponentsBtnConnect(false);
                            CrossThread.SetTextToolStripLabel(statusStrip1, toolStripStatusLabel1, "Error 002. " + errorMessage);
                            return;
                        }
                        double current = powerSupply.ReadCurrent(out isOperationSuccesful, out errorMessage);
                        if (!isOperationSuccesful)
                        {
                            EnableComponentsBtnConnect(false);
                            CrossThread.SetTextToolStripLabel(statusStrip1, toolStripStatusLabel1, "Error 003. " + errorMessage);
                            return;
                        }
                        if (isLimitCurrentEnabled && current >= limitCurrent)
                        {
                            powerSupply.SetOutputModeOFF(out isOperationSuccesful, out errorMessage);
                            powerSupply.ReadExisting(out isOperationSuccesful, out errorMessage);
                        }
                        int outputMode = powerSupply.ReadOutputMode(out isOperationSuccesful, out errorMessage);
                        powerSupply.ReadExisting(out isOperationSuccesful, out errorMessage);
                        if (!isOperationSuccesful)
                        {
                            EnableComponentsBtnConnect(false);
                            CrossThread.SetTextToolStripLabel(statusStrip1, toolStripStatusLabel1, "Error 004. " + errorMessage);

                            return;
                        }

                        if (outputMode == 1)
                        {
                            CrossThread.ChangeCheckBoxStatus(toggleButton, true);
                        }
                        else if (outputMode == 0)
                        {
                            CrossThread.ChangeCheckBoxStatus(toggleButton, false);
                        }
                        CrossThread.SetControlText(labelVoltage, String.Format("{0:0.000} V", voltage));
                        CrossThread.SetControlText(labelCurrent, String.Format("{0:0.000} A", current));
                        CrossThread.SetControlText(labelPower, String.Format("{0:0.000} W", Math.Abs(current * voltage)));                        
                    }
                    catch (Exception ex)
                    {
                        
                        EnableComponentsBtnConnect(false);
                        CrossThread.SetTextToolStripLabel(statusStrip1, toolStripStatusLabel1, ex.Message + " 005");

                    }
                }
                Thread.Sleep(100);
            }
            
        }

        private void buttonApplySetPoints_Click(object sender, EventArgs e)
        {
            bool isOperationSuccessful = true;
            string errorMessage = "";

            double voltage = Convert.ToDouble(numericUpDownVoltage.Value);
            double current = Convert.ToDouble(numericUpDownCurrent.Value);
            readValues = false;
            powerSupply.SetVoltageCurrent(voltage, current, out isOperationSuccessful, out errorMessage);
            if (!isOperationSuccessful)
            {
                MessageBox.Show("Problem sending/recieving data . \n" + errorMessage, "Problem detected",
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                return;
            }
            readValues = true;           
        }

        private void buttonRun_Click_1(object sender, EventArgs e)
        {
            EnableDisableElementsRunning(false);

            cancellationTokenSource = new CancellationTokenSource();

            graphForm = new GraphForm();
            if (radioButtonConstant.Checked == true)
            {               
                setVoltageConstant = Convert.ToDouble(numericUpDownVoltageConstant.Value);
                setMaxCurrentConstant = Convert.ToDouble(numericUpDownCurrentConstant.Value);
                totalRunTime = 60 * Convert.ToDouble(numericUpDownRunTimeConstant.Value);
                daqThread = Task.Factory.StartNew(() => DataAcquisitionConstant(cancellationTokenSource.Token));
            }
            else if (radioButtonSine.Checked == true)
            {
                minVoltageSine = Convert.ToDouble(numericUpDownVMinSine.Value);
                maxVoltageSine = Convert.ToDouble(numericUpDownVMaxSine.Value);
                setMaxCurrentSine = Convert.ToDouble(numericUpDownSetCurrentSine.Value);
                fVoltageSine = Convert.ToDouble(numericUpDownFSine.Value);
                totalRunTime = 60 * Convert.ToDouble(numericUpDownRunTimeSine.Value);
                if (maxVoltageSine < minVoltageSine)
                {
                    MessageBox.Show("Vmax must be greater than Vmin. Correct and retry.");
                    return;
                }
                
                daqThread = Task.Factory.StartNew(() => DataAcquisitionSine(cancellationTokenSource.Token));                
            }
            else if (radioButtonTriangular.Checked == true)
            {
                minVoltageTriangular = Convert.ToDouble(numericUpDownVMinTriangular.Value);
                maxVoltageTriangular = Convert.ToDouble(numericUpDownVMaxTriangular.Value);
                setMaxCurrentTriangular = Convert.ToDouble(numericUpDownSetCurrentTriangular.Value);
                fVoltageTriangular = Convert.ToDouble(numericUpDownFTriangular.Value)/60.0;
                totalRunTime = 60 * Convert.ToDouble(numericUpDownRunTimeTriangular.Value);
                if (maxVoltageTriangular < minVoltageTriangular)
                {
                    MessageBox.Show("Vmax must be greater than Vmin. Correct and retry.");
                    return;
                }
                daqThread = Task.Factory.StartNew(() => DataAcquisitionTriangular(cancellationTokenSource.Token));
            }
            else if (radioButtonRamp.Checked == true)
            {
                minVoltageRamp = Convert.ToDouble(numericUpDownVMinRamp.Value);
                maxVoltageRamp = Convert.ToDouble(numericUpDownVMaxRamp.Value);
                t1Ramp = 60*Convert.ToDouble(numericUpDownT1Ramp.Value);
                t2Ramp = 60*Convert.ToDouble(numericUpDownT2Ramp.Value);
                t3Ramp = 60*Convert.ToDouble(numericUpDownT3Ramp.Value);
                setMaxCurrentRamp = Convert.ToDouble(numericUpDowniMaxRamp.Value);

                if (maxVoltageRamp < minVoltageRamp)
                {
                    MessageBox.Show("Vmax must be greater than Vmin. Correct and retry.");
                    return;
                }
                daqThread = Task.Factory.StartNew(() => DataAcquisitionRamp(cancellationTokenSource.Token));
            }
            else
            {
                return;
            }
            graphForm.Show();
            graphForm.isReceivingData = true;        
            toolStripStatusLabel1.Text = String.Format("Acquiring data from Power Supply...");

        }
        private void DataAcquisitionConstant(CancellationToken ctoken)
        {
            
            bool isOperationSucessful;
            string errorMessage;

            double elapsedTime = 0;
            Stopwatch stopwatch = new Stopwatch();
            readValues = false;

            powerSupply.ReadExisting(out isOperationSucessful, out errorMessage);
            Thread.Sleep(10);
            powerSupply.SetVoltageCurrent(setVoltageConstant,setMaxCurrentConstant, out isOperationSucessful, out errorMessage);
            Thread.Sleep(10);
            if (!isOperationSucessful)
            {
                graphForm.isReceivingData = false;
                powerSupply.SetOutputModeOFF(out isOperationSucessful, out errorMessage);
                EnableDisableElementsRunning(true);
                readValues = true;
                cancellationTokenSource.Cancel();
                return;
            }

            powerSupply.SetOutputModeON(out isOperationSucessful, out errorMessage);
            Thread.Sleep(10);
            if (!isOperationSucessful)
            {
                graphForm.isReceivingData = false;
                powerSupply.SetOutputModeOFF(out isOperationSucessful, out errorMessage);
                EnableDisableElementsRunning(true);
                readValues = true;
                cancellationTokenSource.Cancel();
                return;
            }
            stopwatch.Start();
            while (elapsedTime < totalRunTime && !ctoken.IsCancellationRequested)
            {
                elapsedTime = (double)stopwatch.ElapsedTicks / (double)Stopwatch.Frequency;
                               
                double voltage = powerSupply.ReadVoltage(out isOperationSucessful, out errorMessage);
                Thread.Sleep(10);
                double current = powerSupply.ReadCurrent(out isOperationSucessful, out errorMessage);
                Thread.Sleep(10);
                if (!isOperationSucessful)
                {
                    graphForm.isReceivingData = false;
                    powerSupply.SetOutputModeOFF(out isOperationSucessful, out errorMessage);
                    EnableDisableElementsRunning(true);
                    readValues = true;
                    cancellationTokenSource.Cancel();
                    return;
                }

                graphForm.PlotAppend(elapsedTime/60.0, voltage, current);

                int outputMode = powerSupply.ReadOutputMode(out isOperationSucessful, out errorMessage);
                Thread.Sleep(10);
                if (!isOperationSucessful)
                {
                    graphForm.isReceivingData = false;
                    powerSupply.SetOutputModeOFF(out isOperationSucessful, out errorMessage);
                    EnableDisableElementsRunning(true);
                    readValues = true;
                    cancellationTokenSource.Cancel();
                    return;
                }

                if (outputMode == 1)
                {
                    CrossThread.ChangeCheckBoxStatus(toggleButton, true);
                }
                else if (outputMode == 0)
                {
                    CrossThread.ChangeCheckBoxStatus(toggleButton, false);
                }
                CrossThread.SetControlText(labelVoltage, String.Format("{0:0.000} V", voltage));
                CrossThread.SetControlText(labelCurrent, String.Format("{0:0.000} A", current));
                CrossThread.SetControlText(labelPower, String.Format("{0:0.000} W", Math.Abs(current * voltage)));
                Thread.Sleep(timeInterval);
            }
            if (ctoken.IsCancellationRequested)
            {
                stopwatch.Stop();
                CancelationTokenAction();
                return;
            }

           
            if (!isOperationSucessful)
            {
                graphForm.isReceivingData = false;
                powerSupply.SetOutputModeOFF(out isOperationSucessful, out errorMessage);
                EnableDisableElementsRunning(true);
                readValues = true;
                cancellationTokenSource.Cancel();
                return;
            }
            
            graphForm.isReceivingData = false;
            powerSupply.SetOutputModeOFF(out isOperationSucessful, out errorMessage);
            Thread.Sleep(100);
            EnableDisableElementsRunning(true);            
            readValues = true;
            stopwatch.Stop();
            CrossThread.SetTextToolStripLabel(statusStrip1, toolStripStatusLabel1, "Iddle");
        }

        private void CancelationTokenAction()
        {
            bool isOperationSucessful;
            string errorMessage;
            graphForm.isReceivingData = false;
            Thread.Sleep(10);
            powerSupply.ReadExisting(out isOperationSucessful, out errorMessage);
            Thread.Sleep(10);
            powerSupply.SetOutputModeOFF(out isOperationSucessful, out errorMessage);
            Thread.Sleep(10);
            powerSupply.ReadExisting(out isOperationSucessful, out errorMessage);
            cancellationTokenSource.Dispose();
            Thread.Sleep(200);
            EnableDisableElementsRunning(true);
            readValues = true;
            CrossThread.SetTextToolStripLabel(statusStrip1, toolStripStatusLabel1, "Iddle");
        }

        private void DataAcquisitionSine(CancellationToken ctoken)
        {
            bool isOperationSucessful;
            string errorMessage;
            readValues = false;
            powerSupply.ReadExisting(out isOperationSucessful, out errorMessage);
            Thread.Sleep(10);
            double elapsedTime = 0;
            Stopwatch stopwatch = new Stopwatch();
            double A = (maxVoltageSine - minVoltageSine) / 2.0;
            double setVoltage;
            powerSupply.SetVoltageCurrent(0.0, 0.0, out isOperationSucessful, out errorMessage);
            if (!isOperationSucessful)
            {
                graphForm.isReceivingData = false;
                powerSupply.SetOutputModeOFF(out isOperationSucessful, out errorMessage);
                EnableDisableElementsRunning(true);
                readValues = true;
                cancellationTokenSource.Cancel();
                return;
            }
            powerSupply.SetOutputModeON(out isOperationSucessful, out errorMessage);
            if (!isOperationSucessful)
            {
                graphForm.isReceivingData = false;
                powerSupply.SetOutputModeOFF(out isOperationSucessful, out errorMessage);
                EnableDisableElementsRunning(true);
                readValues = true;
                cancellationTokenSource.Cancel();
                return;
            }
            stopwatch.Start();

            while (elapsedTime < totalRunTime && !ctoken.IsCancellationRequested)
            {
                elapsedTime = (double)stopwatch.ElapsedTicks / (double)Stopwatch.Frequency;
                setVoltage = A * Math.Sin(2.0 * Math.PI * fVoltageSine / 60.0 * elapsedTime - Math.PI / 2.0 ) + A + minVoltageSine;
                powerSupply.SetVoltageCurrent(setVoltage, setMaxCurrentSine, out isOperationSucessful, out errorMessage);
                if (!isOperationSucessful)
                {
                    graphForm.isReceivingData = false;
                    powerSupply.SetOutputModeOFF(out isOperationSucessful, out errorMessage);
                    EnableDisableElementsRunning(true);
                    readValues = true;
                    cancellationTokenSource.Cancel();
                    return;
                }

                double voltage = powerSupply.ReadVoltage(out isOperationSucessful, out errorMessage);
                if (!isOperationSucessful)
                {
                    graphForm.isReceivingData = false;
                    powerSupply.SetOutputModeOFF(out isOperationSucessful, out errorMessage);
                    EnableDisableElementsRunning(true);
                    readValues = true;
                    cancellationTokenSource.Cancel();
                    return;
                }

                double current = powerSupply.ReadCurrent(out isOperationSucessful, out errorMessage);
                if (!isOperationSucessful)
                {
                    graphForm.isReceivingData = false;
                    powerSupply.SetOutputModeOFF(out isOperationSucessful, out errorMessage);
                    EnableDisableElementsRunning(true);
                    readValues = true;
                    cancellationTokenSource.Cancel();
                    return;
                }

                graphForm.PlotAppend(elapsedTime/60.0, voltage, current);

                int outputMode = powerSupply.ReadOutputMode(out isOperationSucessful, out errorMessage);
                if (!isOperationSucessful)
                {
                    graphForm.isReceivingData = false;
                    powerSupply.SetOutputModeOFF(out isOperationSucessful, out errorMessage);
                    EnableDisableElementsRunning(true);
                    readValues = true;
                    cancellationTokenSource.Cancel();
                    return;
                }

                if (outputMode == 1)
                {
                    CrossThread.ChangeCheckBoxStatus(toggleButton, true);
                }
                else if (outputMode == 0)
                {
                    CrossThread.ChangeCheckBoxStatus(toggleButton, false);
                }

                CrossThread.SetControlText(labelVoltage, String.Format("{0:0.000} V", voltage));
                CrossThread.SetControlText(labelCurrent, String.Format("{0:0.000} A", current));
                CrossThread.SetControlText(labelPower, String.Format("{0:0.000} W", Math.Abs(current * voltage)));
                Thread.Sleep(timeInterval);
            }
            graphForm.isReceivingData = false;
            powerSupply.SetOutputModeOFF(out isOperationSucessful, out errorMessage);
            Thread.Sleep(100);
            EnableDisableElementsRunning(true);
            readValues = true;
            stopwatch.Stop();

            CrossThread.SetTextToolStripLabel(statusStrip1, toolStripStatusLabel1, "Iddle");


            if (!isOperationSucessful)
            {
                graphForm.isReceivingData = false;
                powerSupply.SetOutputModeOFF(out isOperationSucessful, out errorMessage);
                EnableDisableElementsRunning(true);
                readValues = true;
                cancellationTokenSource.Cancel();
                return;
            }

            if (ctoken.IsCancellationRequested)
            {
                stopwatch.Stop();
                CancelationTokenAction();
                return;
            }
        }
        private void DataAcquisitionTriangular(CancellationToken ctoken)
        {
            bool isOperationSucessful;
            string errorMessage;

            double elapsedTime = 0;
            Stopwatch stopwatch = new Stopwatch();
            double setVoltage;

            readValues = false;
            powerSupply.ReadExisting(out isOperationSucessful, out errorMessage);
            Thread.Sleep(10);
            powerSupply.SetVoltageCurrent(0.0, 0.0, out isOperationSucessful, out errorMessage);
            if (!isOperationSucessful)
            {
                graphForm.isReceivingData = false;
                powerSupply.SetOutputModeOFF(out isOperationSucessful, out errorMessage);
                EnableDisableElementsRunning(true);
                readValues = true;
                cancellationTokenSource.Cancel();
                return;
            }
            powerSupply.SetOutputModeON(out isOperationSucessful, out errorMessage);

            if (!isOperationSucessful)
            {
                graphForm.isReceivingData = false;
                powerSupply.SetOutputModeOFF(out isOperationSucessful, out errorMessage);
                EnableDisableElementsRunning(true);
                readValues = true;
                cancellationTokenSource.Cancel();
                return;
            }
            stopwatch.Start();
            double period = 1 / fVoltageTriangular;           
            double A = (maxVoltageTriangular - minVoltageTriangular) / 2.0;

            while (elapsedTime < totalRunTime && !ctoken.IsCancellationRequested)
            {                
                elapsedTime = (double)stopwatch.ElapsedTicks / (double)Stopwatch.Frequency;
                setVoltage = (2.0 * A / Math.PI) * Math.Asin(Math.Sin(2.0 * Math.PI / period * elapsedTime - Math.PI / 2.0)) + A + minVoltageTriangular; ;
                powerSupply.SetVoltageCurrent(setVoltage, setMaxCurrentTriangular, out isOperationSucessful, out errorMessage);
                if (!isOperationSucessful)
                {
                    graphForm.isReceivingData = false;
                    powerSupply.SetOutputModeOFF(out isOperationSucessful, out errorMessage);
                    EnableDisableElementsRunning(true);
                    readValues = true;
                    CrossThread.SetTextToolStripLabel(statusStrip1, toolStripStatusLabel1, errorMessage);
                    return;
                }
                double voltage = powerSupply.ReadVoltage(out isOperationSucessful, out errorMessage);
                if (!isOperationSucessful)
                {
                    graphForm.isReceivingData = false;
                    powerSupply.SetOutputModeOFF(out isOperationSucessful, out errorMessage);
                    EnableDisableElementsRunning(true);
                    readValues = true;
                    cancellationTokenSource.Cancel();
                    CrossThread.SetTextToolStripLabel(statusStrip1, toolStripStatusLabel1, errorMessage);
                    return;
                }
                double current = powerSupply.ReadCurrent(out isOperationSucessful, out errorMessage);
                if (!isOperationSucessful)
                {
                    graphForm.isReceivingData = false;
                    powerSupply.SetOutputModeOFF(out isOperationSucessful, out errorMessage);
                    EnableDisableElementsRunning(true);
                    readValues = true;
                    cancellationTokenSource.Cancel();                    
                    CrossThread.SetTextToolStripLabel(statusStrip1, toolStripStatusLabel1, errorMessage);
                    return;
                }
                graphForm.PlotAppend(elapsedTime / 60.0, voltage, current);
                int outputMode = powerSupply.ReadOutputMode(out isOperationSucessful, out errorMessage);
                if (!isOperationSucessful)
                {
                    graphForm.isReceivingData = false;
                    powerSupply.SetOutputModeOFF(out isOperationSucessful, out errorMessage);
                    EnableDisableElementsRunning(true);
                    readValues = true;
                    cancellationTokenSource.Cancel();
                    CrossThread.SetTextToolStripLabel(statusStrip1, toolStripStatusLabel1, errorMessage);
                    return;
                }
                if (outputMode == 1)
                {
                    CrossThread.ChangeCheckBoxStatus(toggleButton, true);
                }
                else if (outputMode == 0)
                {
                    CrossThread.ChangeCheckBoxStatus(toggleButton, false);
                }
                CrossThread.SetControlText(labelVoltage, String.Format("{0:0.000} V", voltage));
                CrossThread.SetControlText(labelCurrent, String.Format("{0:0.000} A", current));
                CrossThread.SetControlText(labelPower, String.Format("{0:0.000} W", Math.Abs(current * voltage)));
                Thread.Sleep(timeInterval);
            }
            graphForm.isReceivingData = false;
            powerSupply.SetOutputModeOFF(out isOperationSucessful, out errorMessage);
            Thread.Sleep(100);
            EnableDisableElementsRunning(true);
            readValues = true;
            stopwatch.Stop();

            CrossThread.SetTextToolStripLabel(statusStrip1, toolStripStatusLabel1, "Iddle");

            if (!isOperationSucessful)
            {
                graphForm.isReceivingData = false;
                powerSupply.SetOutputModeOFF(out isOperationSucessful, out errorMessage);
                EnableDisableElementsRunning(true);
                readValues = true;
                cancellationTokenSource.Cancel();
                CrossThread.SetTextToolStripLabel(statusStrip1, toolStripStatusLabel1, errorMessage);
                return;
            }
            if (ctoken.IsCancellationRequested)
            {
                stopwatch.Stop();
                CancelationTokenAction();
                return;
            }
        }

        private void DataAcquisitionRamp(CancellationToken ctoken)
        {
            bool isOperationSucessful;
            string errorMessage;

            double elapsedTime = 0;
            Stopwatch stopwatch = new Stopwatch();
            readValues = false;

            double m1Ramp = (maxVoltageRamp - minVoltageRamp) / t1Ramp;
            double m2Ramp = (minVoltageRamp - maxVoltageRamp) / (t3Ramp - t2Ramp);
            double setVoltage;
            powerSupply.ReadExisting(out isOperationSucessful, out errorMessage);
            Thread.Sleep(10);
            powerSupply.SetVoltageCurrent(0.0, 0.0, out isOperationSucessful, out errorMessage);
            if (!isOperationSucessful)
            {
                graphForm.isReceivingData = false;
                powerSupply.SetOutputModeOFF(out isOperationSucessful, out errorMessage);
                EnableDisableElementsRunning(true);
                readValues = true;
                cancellationTokenSource.Cancel();
                return;
            }
            powerSupply.SetOutputModeON(out isOperationSucessful, out errorMessage);
            if (!isOperationSucessful)
            {
                graphForm.isReceivingData = false;
                powerSupply.SetOutputModeOFF(out isOperationSucessful, out errorMessage);
                EnableDisableElementsRunning(true);
                readValues = true;
                cancellationTokenSource.Cancel();
                return;
            }
            stopwatch.Start();

            while (elapsedTime < t3Ramp && !ctoken.IsCancellationRequested)
            {
                elapsedTime = (double)stopwatch.ElapsedTicks / (double)Stopwatch.Frequency;

                if (elapsedTime < t1Ramp)
                {
                    setVoltage = minVoltageRamp + m1Ramp * elapsedTime;
                }
                else if (elapsedTime >= t1Ramp && elapsedTime < t2Ramp)
                {
                    setVoltage = maxVoltageRamp;
                }
                else if (elapsedTime >= t2Ramp && elapsedTime < t3Ramp)
                {
                    setVoltage = maxVoltageRamp-m2Ramp*t2Ramp + m2Ramp*elapsedTime;
                }
                else
                {
                    setVoltage = minVoltageRamp;
                }

                powerSupply.SetVoltageCurrent(setVoltage, setMaxCurrentRamp, out isOperationSucessful, out errorMessage);
                if (!isOperationSucessful)
                {
                    graphForm.isReceivingData = false;
                    powerSupply.SetOutputModeOFF(out isOperationSucessful, out errorMessage);
                    EnableDisableElementsRunning(true);
                    readValues = true;
                    cancellationTokenSource.Cancel();
                    return;
                }

                double voltage = powerSupply.ReadVoltage(out isOperationSucessful, out errorMessage);
                if (!isOperationSucessful)
                {
                    graphForm.isReceivingData = false;
                    powerSupply.SetOutputModeOFF(out isOperationSucessful, out errorMessage);
                    EnableDisableElementsRunning(true);
                    readValues = true;
                    cancellationTokenSource.Cancel();
                    return;
                }

                double current = powerSupply.ReadCurrent(out isOperationSucessful, out errorMessage);
                if (!isOperationSucessful)
                {
                    graphForm.isReceivingData = false;
                    powerSupply.SetOutputModeOFF(out isOperationSucessful, out errorMessage);
                    EnableDisableElementsRunning(true);
                    readValues = true;
                    cancellationTokenSource.Cancel();
                    return;
                }

                graphForm.PlotAppend(elapsedTime / 60.0, voltage, current);

                int outputMode = powerSupply.ReadOutputMode(out isOperationSucessful, out errorMessage);
                if (!isOperationSucessful)
                {
                    graphForm.isReceivingData = false;
                    powerSupply.SetOutputModeOFF(out isOperationSucessful, out errorMessage);
                    EnableDisableElementsRunning(true);
                    readValues = true;
                    cancellationTokenSource.Cancel();
                    return;
                }

                if (outputMode == 1)
                {
                    CrossThread.ChangeCheckBoxStatus(toggleButton, true);
                }
                else if (outputMode == 0)
                {
                    CrossThread.ChangeCheckBoxStatus(toggleButton, false);
                }

                CrossThread.SetControlText(labelVoltage, String.Format("{0:0.000} V", voltage));
                CrossThread.SetControlText(labelCurrent, String.Format("{0:0.000} A", current));
                CrossThread.SetControlText(labelPower, String.Format("{0:0.000} W", Math.Abs(current * voltage)));
                Thread.Sleep(timeInterval);
            }
            graphForm.isReceivingData = false;
            powerSupply.SetOutputModeOFF(out isOperationSucessful, out errorMessage);
            Thread.Sleep(100);
            EnableDisableElementsRunning(true);
            readValues = true;
            stopwatch.Stop();
            CrossThread.SetTextToolStripLabel(statusStrip1, toolStripStatusLabel1, "Iddle");

            powerSupply.SetOutputModeOFF(out isOperationSucessful, out errorMessage);
            if (!isOperationSucessful)
            {
                graphForm.isReceivingData = false;
                powerSupply.SetOutputModeOFF(out isOperationSucessful, out errorMessage);
                EnableDisableElementsRunning(true);
                readValues = true;
                cancellationTokenSource.Cancel();
                return;
            }
            if (ctoken.IsCancellationRequested)
            {
                stopwatch.Stop();
                CancelationTokenAction();
                return;
            }
        }

        private void EnableDisableElementsRunning(bool enableOrDisable) 
        {
            //CrossThread.EnableControl(groupBoxProgram, false);
            CrossThread.EnableControl(buttonRun, enableOrDisable);
            CrossThread.EnableControl(groupBoxSettings, enableOrDisable);
            CrossThread.EnableControl(buttonRun, enableOrDisable);
            CrossThread.EnableControl(buttonStop,!enableOrDisable);
        }


        private void toolStripButtonReloadPorts_Click(object sender, EventArgs e)
        {
            toolStripComboBoxPortNames.ComboBox.SelectedItem = null;
            toolStripComboBoxPortNames.ComboBox.DataSource = null;        
            toolStripComboBoxPortNames.ComboBox.Items.Clear();
            toolStripComboBoxPortNames.ComboBox.ResetText();
            toolStripComboBoxPortNames.ComboBox.DataSource = SerialPort.GetPortNames();
        }

        private void EnableDisableConstantTools(bool enableOrDisable)
        {
            numericUpDownVoltageConstant.Enabled = enableOrDisable;
            numericUpDownCurrentConstant.Enabled = enableOrDisable;
            numericUpDownRunTimeConstant.Enabled = enableOrDisable;
        }


        private void buttonStop_Click_1(object sender, EventArgs e)
        {
            bool isOperationSuccessful;
            string errorMessage;
            try
            {
                cancellationTokenSource.Cancel();

                Thread.Sleep(200);
                
                readValues = true;
                toolStripStatusLabel1.Text = "";
                powerSupply.ReadExisting(out isOperationSuccessful, out errorMessage);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Problem detected", MessageBoxButtons.OK, 
                    MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
            }
        }

        private void radioButtonSine_CheckedChanged(object sender, EventArgs e)
        {
            EnablePrograms();
        }        

        private void radioButtonConstant_CheckedChanged(object sender, EventArgs e)
        {
            EnablePrograms();
        }

        private void radioButtonTriangular_CheckedChanged(object sender, EventArgs e)
        {
            EnablePrograms();
        }

        private void radioButtonRamp_CheckedChanged(object sender, EventArgs e)
        {
            EnablePrograms();
        }
        private void EnablePrograms()
        {
            pictureBoxConstant.Enabled = radioButtonConstant.Checked;
            numericUpDownCurrentConstant.Enabled = radioButtonConstant.Checked;
            numericUpDownVoltageConstant.Enabled = radioButtonConstant.Checked;
            numericUpDownRunTimeConstant.Enabled = radioButtonConstant.Checked;
            labelImaxConstant.Enabled = radioButtonConstant.Checked;
            labelVConstant.Enabled = radioButtonConstant.Checked;
            labelTimeConstant.Enabled = radioButtonConstant.Checked;

            pictureBoxSine.Enabled = radioButtonSine.Checked;
            numericUpDownVMinSine.Enabled = radioButtonSine.Checked;
            numericUpDownVMaxSine.Enabled = radioButtonSine.Checked;
            numericUpDownFSine.Enabled = radioButtonSine.Checked; ;
            numericUpDownRunTimeSine.Enabled = radioButtonSine.Checked;
            numericUpDownSetCurrentSine.Enabled = radioButtonSine.Checked;
            label1IMaxSine.Enabled = radioButtonSine.Checked;
            label1TimeSine.Enabled = radioButtonSine.Checked;
            labelFCSine.Enabled = radioButtonSine.Checked;
            labelVMaxSine.Enabled = radioButtonSine.Checked;
            labelVminSine.Enabled = radioButtonSine.Checked;
            
            pictureBoxTriangular.Enabled = radioButtonTriangular.Checked;
            numericUpDownVMaxTriangular.Enabled = radioButtonTriangular.Checked;
            numericUpDownVMinTriangular.Enabled = radioButtonTriangular.Checked;
            numericUpDownRunTimeTriangular.Enabled = radioButtonTriangular.Checked;
            numericUpDownFTriangular.Enabled = radioButtonTriangular.Checked;            
            numericUpDownSetCurrentTriangular.Enabled = radioButtonTriangular.Checked;
            label1FTriangle.Enabled = radioButtonTriangular.Checked;
            label1VMaxTriangle.Enabled = radioButtonTriangular.Checked;
            labelIMaxTriangle.Enabled = radioButtonTriangular.Checked;
            labelTimeTriangle.Enabled = radioButtonTriangular.Checked;
            labelVMinTriangle.Enabled = radioButtonTriangular.Checked;

            pictureBoxRamp.Enabled = radioButtonRamp.Checked;
            numericUpDowniMaxRamp.Enabled = radioButtonRamp.Checked;
            numericUpDownT1Ramp.Enabled = radioButtonRamp.Checked;
            numericUpDownT2Ramp.Enabled = radioButtonRamp.Checked;
            numericUpDownT3Ramp.Enabled = radioButtonRamp.Checked;
            numericUpDownVMaxRamp.Enabled = radioButtonRamp.Checked;
            numericUpDownVMinRamp.Enabled = radioButtonRamp.Checked;
            label1IMaxRamp.Enabled = radioButtonRamp.Checked;
            label1VMaxRamp.Enabled = radioButtonRamp.Checked;
            labelT1Ramp.Enabled = radioButtonRamp.Checked;
            labelt2Ramp.Enabled = radioButtonRamp.Checked;
            labelt3Ramp.Enabled = radioButtonRamp.Checked;
            labelVMinRamp.Enabled = radioButtonRamp.Checked;
        }

        private void numericUpDownT1Ramp_ValueChanged(object sender, EventArgs e)
        {
            SetMaxMinValuesTimeRamp();
        }
        
        private void numericUpDownT2Ramp_ValueChanged(object sender, EventArgs e)
        {
            SetMaxMinValuesTimeRamp();
        }

        private void numericUpDownT3Ramp_ValueChanged(object sender, EventArgs e)
        {
            SetMaxMinValuesTimeRamp();
        }
        private void SetMaxMinValuesTimeRamp()
        {
            numericUpDownT2Ramp.Minimum = numericUpDownT1Ramp.Value;
            numericUpDownT3Ramp.Minimum = numericUpDownT2Ramp.Value;
        }

        private void checkBoxCurrentLimit_CheckedChanged(object sender, EventArgs e)
        {
            isLimitCurrentEnabled = checkBoxCurrentLimit.Checked;
            limitCurrent = Convert.ToDouble(numericUpDowniLimit.Value);
        }

        private void numericUpDowniLimit_ValueChanged(object sender, EventArgs e)
        {
            limitCurrent = Convert.ToDouble(numericUpDowniLimit.Value);
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            AboutBox1 box = new AboutBox1();
            box.ShowDialog();
        }
    }

    

}
