using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;

namespace NMEA_Signal_Generator
{
    public partial class Form1 : Form
    {
        #region VARIABLES

        //Private Variables
        private SerialPort port = new SerialPort();
        private Size defaultSize = new Size(580, 574);
        private Size defaultSizeWithCustom = new Size(987, 574);

        private bool stop = false;
        private bool connected = false;
        private bool _isRunning = false;
        private int runningThreads = 0;

        //Arrays
        private string[] dataBits = new string[4] { "5", "6", "7", "8" };
        private string[] baudRate = new string[4] { "4800", "9600", "19200", "38400" };
        private string[] parity = new string[5] { "NONE", "ODD", "EVEN", "MARK", "SPACE" };
        private string[] stopBits = new string[4] { "1", "1.5", "2", "NONE" };
        private string[] handshaking = new string[5] { "NONE", "RTS/CTS", "XON/XOFF", "RTS/CTS + XON/XOFF", "RTS on TX" };
        private string[] NMEAProtocol = new string[4] { "HDG", "HDT", "VBW", "TEST" };
        private string[] testData = new string[13];

        //Delegates
        delegate void SetRichTextCallback(RichTextBox richTextBox, string text);
        delegate void SetLabelTextCallback(Label label, string text);

        #endregion

        #region INIT
        public Form1()
        {
            InitializeComponent();
            Init();
        }
        private void Init()
        {
            //set default Form size
            //gbNMEA.Hide();
            this.Size = defaultSize;
            this.Refresh();

            //Load data into combo boxes
            var ports = SerialPort.GetPortNames();
            cbCom.DataSource = ports;
            cbDataBits.Items.AddRange(dataBits);
            cbBaudRate.Items.AddRange(baudRate);
            cbFlow.Items.AddRange(handshaking);
            cbStopBits.Items.AddRange(stopBits);
            cbParity.Items.AddRange(parity);
            cbNMEA.Items.AddRange(NMEAProtocol);

            //set default values for all controls
            cbDataBits.SelectedIndex = 3;
            cbBaudRate.SelectedIndex = 0;
            cbFlow.SelectedIndex = 0;
            cbStopBits.SelectedIndex = 0;
            cbParity.SelectedIndex = 0;
            cbInc.SelectedIndex = 5;
            cbNMEA.SelectedIndex = 1;
            tbEnd.Text = "200";
            tbStart.Text = "100";
            tbTime.Text = "1000";
            tbHeader.Text = "HE";
            t1Enabled.Checked = true;

            //Enable or Disable controls
            ckbNMEA.Enabled = false;
            cbNav.Enabled = false;

            foreach (CheckBox cb in groupBox4.Controls.OfType<CheckBox>())
            {
                if ((cb.Text == "Reverse" || cb.Text == "ReturnToStart") && !cb.Name.Contains("t1"))
                {
                    cb.Enabled = false;
                }

            }

            foreach (TextBox tb in groupBox4.Controls.OfType<TextBox>())
            {
                if (tb.Text == "")
                {
                    tb.Enabled = false;
                }

            }

        }

        #endregion

        #region CONTROLS
        private void btnConnect_Click(object sender, EventArgs e)
        {
            rtbConsole.AppendText("Attempting to connect to port: " + cbCom.SelectedItem.ToString());
            rtbConsole.AppendText(Environment.NewLine);
            try
            {
                #region Com Port Settings

                port.PortName = cbCom.SelectedItem.ToString();
                port.DataBits = Convert.ToInt32(cbDataBits.SelectedItem.ToString());
                port.BaudRate = Convert.ToInt32(cbBaudRate.SelectedItem.ToString());
                port.NewLine = "\n";

                string parity = cbParity.SelectedItem.ToString(); //"NONE", "ODD",  "EVEN", "MARK", "SPACE"
                string stopBits = cbStopBits.SelectedItem.ToString(); // "1", "1.5", "2"
                string handShake = cbFlow.SelectedItem.ToString(); // "NONE", "RTS/CTS", "XON/XOFF", "RTS/CTS + XON/XOFF", "RTS on TX"

                //Parity
                if (parity == "NONE")
                {
                    port.Parity = Parity.None;
                }
                else if (parity == "ODD")
                {
                    port.Parity = Parity.Odd;
                }
                else if (parity == "EVEN")
                {
                    port.Parity = Parity.Even;
                }
                else if (parity == "MARK")
                {
                    port.Parity = Parity.Mark;
                }
                else if (parity == "SPACE")
                {
                    port.Parity = Parity.Space;
                }

                //StopBits               
                if (stopBits == "NONE")
                {
                    port.StopBits = StopBits.None;
                }
                else if (stopBits == "1")
                {
                    port.StopBits = StopBits.One;
                }
                else if (stopBits == "1.5")
                {
                    port.StopBits = StopBits.OnePointFive;
                }
                else if (stopBits == "2")
                {
                    port.StopBits = StopBits.Two;
                }

                //HandShake "NONE", "RTS/CTS", "XON/XOFF", "RTS/CTS + XON/XOFF", "RTS on TX"
                if (handShake == "NONE")
                {
                    port.Handshake = Handshake.None;
                }
                else if (handShake == "RTS/CTS")
                {
                    port.Handshake = Handshake.RequestToSend;
                }
                else if (handShake == "RTS/CTS + XON/XOFF")
                {
                    port.Handshake = Handshake.RequestToSendXOnXOff;
                }
                else if (handShake == "XON/XOFF")
                {
                    port.Handshake = Handshake.XOnXOff;
                }
                else if (handShake == "RTS on TX")
                {
                    port.RtsEnable = true;
                }

                #endregion

                port.Open();
                AppendText(rtbConsole, "Connected to " + cbCom.SelectedItem.ToString());

                //disable controls so you cant try and connect again
                foreach (Control control in groupBox1.Controls)
                {
                    control.Enabled = control.Text.Contains("Disconnect") ? true : false;
                }
                _comStatus.Text = "Connected";
                _comStatus.ForeColor = Color.Green;
                connected = true;
            }
            catch (Exception ex)
            {
                AppendText(rtbConsole, "Unable to Connect");
                AppendText(rtbConsole, ex.ToString());
                _comStatus.Text = "ERROR";
                _comStatus.ForeColor = Color.Red;
            }

        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            //re-enable controls
            foreach (Control control in groupBox1.Controls)
            {
                control.Enabled = true;
            }

            try
            {
                port.Close();
                AppendText(rtbConsole, "Com Port " + cbCom.SelectedItem.ToString() + " Closed");
                connected = false;
                _comStatus.Text = "Disconnected";
                _comStatus.ForeColor = Color.Black;
            }
            catch
            {
                AppendText(rtbConsole, "Unable to close com port" + Environment.NewLine);
                _comStatus.Text = "DISCONNECT ERROR";
                _comStatus.ForeColor = Color.Red;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                //if user closes the program, attempt to close port
                port.Close();
            }
            catch
            {

            }
        }

        private void ckbNMEA_CheckedChanged(object sender, EventArgs e)
        {
            if (ckbNMEA.Checked == true)
            {
                //this.Size = new Size(586, 834);
                //gbNMEA.Show();
                gbConsole.Location = new Point(12, 607);
                this.Refresh();
            }
            else
            {
                //gbNMEA.Hide();
                // this.Size = new Size(586, 536);
                gbConsole.Location = new Point(12, 309);
                this.Refresh();
            }
        }

        private void btnSendData_Click(object sender, EventArgs e)
        {
            try//store all variables in identifiers and check the the data is valid
            {
                string NMEAType = cbNMEA.SelectedItem.ToString();
                double startNum = Convert.ToDouble(tbStart.Text.ToString());
                double endNum = Convert.ToDouble(tbEnd.Text.ToString());
                double add = Convert.ToDouble(cbInc.SelectedItem.ToString());
                int time = Convert.ToInt32(tbTime.Text.ToString());
                string header = tbHeader.Text.ToString();
                bool reverse = cbReverseN.Checked;
                bool rts = cbRts.Checked;
                bool repeat = cbCont.Checked;

                new Thread(() =>
                {
                    runningThreads++;
                    setLabelText(lbThreads, runningThreads.ToString());
                    Thread.CurrentThread.IsBackground = true;
                    GenerateData(NMEAType, time, startNum, endNum, add, header, reverse, rts, repeat,0);
                }).Start();
            }
            catch (Exception ex)
            {
                AppendText(rtbConsole, "Not all of the infomation was entered to complete the request");
            }
        }
        private void rtbConsole_TextChanged(object sender, EventArgs e)
        {
            rtbConsole.ScrollToCaret();
        }

        private void btnStopData_Click(object sender, EventArgs e)
        {
            _isRunning = false;
            stop = true;
            if(runningThreads >= 0)
            {
                runningThreads--;
                setLabelText(lbThreads, runningThreads.ToString());
            }
        }

        private void cbCustom_CheckedChanged(object sender, EventArgs e)
        {
            if (cbCustom.Checked == true)
            {
                this.Size = defaultSizeWithCustom;
                foreach (Control control in groupBox2.Controls)
                {
                    control.Enabled = false;
                }
                cbCustom.Enabled = true;
                cbNMEA.Enabled = true;
                cbReverseN.Checked = false;

                this.Refresh();
            }
            else
            {
                foreach (Control control in groupBox2.Controls)
                {
                    control.Enabled = control.Text.Contains("Show") ? false : true;
                }

                this.Size = defaultSize;
                this.Refresh();
            }


        }

        private void cbNav_CheckedChanged(object sender, EventArgs e)
        {
            if (cbNav.Checked == true)
            {
                this.Size = new Size(1435, 536);
                //gbNMEA.Show();
                //gbConsole.Location = new Point(12, 607);
                this.Refresh();
            }
            else
            {
                //gbNMEA.Hide();
                this.Size = new Size(586, 536);
                // gbConsole.Location = new Point(12, 309);
                this.Refresh();
            }
        }

        private void btnStartTest_Click(object sender, EventArgs e)
        {
            int step = 0;
            try//store all data in strnig array and check the the data is valid
            {
                foreach (TextBox tb in groupBox4.Controls.OfType<TextBox>())
                {
                    if (tb.Enabled == true)
                    {

                        string temp = tb.Text.ToString();
                        string _reverse = "false";
                        string _rts = "false";
                        

                        //if enabled find corresponding Checkbox for reverse and rts
                        foreach (CheckBox cb in groupBox4.Controls.OfType<CheckBox>())
                        {
                            if (cb.Name.Contains(tb.Name.ToString()) && cb.Name.Contains("Reverse") && cb.Checked == true)
                            {
                                _reverse = "true";
                            }
                            if (cb.Name.Contains(tb.Name.ToString()) && cb.Name.Contains("rts") && cb.Checked == true)
                            {
                                _rts = "true";
                            }
                        }

                        testData[step] = temp + "," + _reverse + "," + _rts;
                        step++;
                    }
                }
                //itterate through all of the data and run the test
                new Thread(() =>
                {
                    Thread.CurrentThread.IsBackground = true;

                    int t = 1;
                    for (int j = testData.Length - 1; j >= 0; j--)
                    {
                        string str = testData[j];
                        if (str != null)
                        {
                            try
                            {
                                //Ittereate through Textboxes
                                string[] array = str.Split(',');

                                //HARDCODED
                                int x = Convert.ToBoolean(array[4]) ? 2 : 1;//if rts true then divide total time in half
                                string NMEAType = "HDT";
                                double startNum = Convert.ToDouble(array[1]);
                                double endNum = Convert.ToDouble(array[2]);
                                double add = 1;
                                double calcTime = ((Convert.ToDouble(array[0]) * 1000) / Math.Abs(startNum - endNum)) / x;
                                int time = Convert.ToInt32(calcTime);
                                string header = "HE";
                                bool repeat = cbRepeat.Checked;
                                int timer = Convert.ToInt32(array[3]);

                                GenerateData(NMEAType, time, startNum, endNum, add, header, Convert.ToBoolean(array[4]), Convert.ToBoolean(array[5]),repeat,timer);
                                // Thread.Sleep(Convert.ToInt32((Convert.ToDouble(array[0]) * 1000))); //give time for thread to execute
                                Thread.Sleep(Convert.ToInt32(array[3]));
                                AppendText(rtbConsole, "----- TEST NO." + t + " COMPLETE -----");
                                t++;
                            }
                            catch
                            {

                            }
                            //AppendTextBox(time.ToString());
                        }

                    }
                }).Start();


            }
            catch (Exception ex)
            {
                AppendText(rtbConsole, ex.ToString());
                AppendText(rtbConsole, "Not all of the infomation was entered to complete the request");

            }
        }

        private void btnStopTest_Click(object sender, EventArgs e)
        {
            stop = true;
        }

        private void checkBox_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox cb = sender as CheckBox;

            foreach (TextBox tb in groupBox4.Controls.OfType<TextBox>())
            {
                if (tb.Name.Contains(cb.Name.ToString().Remove(2)))
                {
                    tb.Enabled = cb.Checked ? true : false;
                }
            }

            foreach (CheckBox cb2 in groupBox4.Controls.OfType<CheckBox>())
            {
                if ((cb2.Name.Contains("Reverse") || cb2.Name.Contains("rts")) && cb2.Name.Contains(cb.Name.ToString().Remove(2)))
                {
                    cb2.Enabled = cb.Checked ? true : false;
                }
            }
        }

        private void rtbClear_Click(object sender, EventArgs e)
        {
            rtbConsole.Clear();
        }

        #endregion  

        #region Functions

        //Runs around 10 times a second.
        public void GenerateData(string _type, int timer, double start, double end, double add, string header, bool reverse, bool rts, bool repeat, int pauseTime)
        {
            string data = "";
            string CRC = "*00";//default value
            bool loop = false;
            bool endFWDThread = false;
            bool endREVThread = false;
            double step = 0;

            //KEEP TRACK OF RTS AND REVERSE
            bool _reverse = reverse;
            bool _rts = rts;

            //set staus of running threads
            //lbThreads.Text = runningThreads.ToString();

            if (_isRunning == true)
            {
                runningThreads--;
                setLabelText(lbThreads, runningThreads.ToString());
                return;//exit if it is already running
            }
            else //continue
            {
                if (_type == "HDT")//Heading True
                {
                    _isRunning = true;//Set the program command flag to setting

                    STARTINIT:
                    if (reverse == true)
                    {
                        goto HDT_CCW; //clockwise FWD
                    }
                    else
                    {
                        goto HDT_CW; //counterclockwise REV
                    }

                    #region CW
                    HDT_CW:
                    step = start;
                    new Thread(() =>
                    {
                        Thread.CurrentThread.IsBackground = true;
                        runningThreads++;
                        setLabelText(lbThreads, runningThreads.ToString());
                        while (true)
                        {
                            if (stop == true || endFWDThread == true)
                            {
                                stop = false;
                                endFWDThread = false;
                                runningThreads--;
                                setLabelText(lbThreads, runningThreads.ToString());
                                break;//exit loop
                            }
                            else
                            {
                                data = header + "HDT," + Math.Round(step, 2) + ",T";
                                CRC = xorIt(data);
                                AppendText(rtbConsole, "$" + data + "*" + CRC);
                                //if serial connected send the data
                                if (connected == true)
                                {
                                    port.Write("$" + data + "*" + CRC);
                                    port.Write(new byte[] { 13, 10 }, 0, 2);
                                }
                                Thread.Sleep(100);
                            }
                        }

                    }).Start();

                    if (start > end)
                    {
                        Thread.CurrentThread.IsBackground = true;
                        //count up to 360
                        for (double i = start; i < 360.00; i += add)
                        {
                            step += add;
                            Thread.Sleep(timer);
                        }
                        step = 0; //reset back to zero
                        for (double i = 0; i < end; i += add)
                        {
                            step += add;
                            Thread.Sleep(timer);
                        }
                    }
                    else //end > start
                    {
                        Thread.CurrentThread.IsBackground = true;
                        //count up to 360
                        for (double i = start; i < end; i += add)
                        {
                            step += add;
                            Thread.Sleep(timer);
                        }
                        
                    }
                    Thread.Sleep(pauseTime);
                    endFWDThread = true;

                    goto RETURNTOSTART;
                    #endregion 

                    #region CCW
                    HDT_CCW:
                    step = end;
                    new Thread(() =>
                    {
                        runningThreads++;
                        setLabelText(lbThreads, runningThreads.ToString());
                        Thread.CurrentThread.IsBackground = true;
                        while (true)
                        {
                            if (stop == true || endREVThread == true)
                            {
                                stop = false;
                                endFWDThread = false;
                                runningThreads--;
                                setLabelText(lbThreads, runningThreads.ToString());
                                break;//exit loop
                            }
                            else
                            {
                                data = header + "HDT," + Math.Round(step, 2) + ",T";
                                CRC = xorIt(data);
                                AppendText(rtbConsole, "$" + data + "*" + CRC);
                                //if serial connected send the data
                                if (connected == true)
                                {
                                    port.Write("$" + data + "*" + CRC);
                                    port.Write(new byte[] { 13, 10 }, 0, 2);
                                }
                                Thread.Sleep(100);
                            }
                        }

                    }).Start();


                    step = end;
                    if (start > end)
                    {
                        for (double i = end; i > 0; i -= add)
                        {
                            step -= add;
                            Thread.Sleep(timer);
                        }
                        step = 360; //reset back to zero
                        for (double i = 360; i > start; i -= add)
                        {
                            step -= add;
                            Thread.Sleep(timer);
                        }
                    }
                    else
                    {
                        step = end;
                        for (double i = end; i > start; i -= add)
                        {
                            step -= add;
                            Thread.Sleep(timer);
                        }
                    }
                    Thread.Sleep(pauseTime);
                    endREVThread = true;

                    goto RETURNTOSTART;

                    #endregion

                    RETURNTOSTART:
                    if (rts == true && reverse == false && repeat == false) //Check if we need to run in reverse
                    {
                        //double temp = start;
                        //start = end;
                        //end = temp;
                        rts = false;
                        reverse = true;
                        goto STARTINIT;
                    }
                    else if (rts == true && reverse == true && repeat == false)
                    {
                        rts = false;
                        reverse = false;
                        goto STARTINIT;
                    }
                    else if (repeat == true)
                    {
                        if(_rts == true)
                        {
                            rts = !rts;
                            reverse = !reverse;
                        }
                        else if(_reverse == true)
                        {
                            reverse = !reverse;
                        }
                        goto STARTINIT;
                    }

                }

                else if (_type == "VBW")
                {
                    new Thread(() =>
                    {
                        Thread.CurrentThread.IsBackground = true;
                        while (true)
                        {
                            if (stop == true || endFWDThread == true)
                            {
                                stop = false;
                                _isRunning = false;
                                endFWDThread = false;

                                break;//exit loop
                            }
                            else
                            {
                                //$--VBW,x.x,x.x,A,x.x,x.x,A*hh<CR><LF>
                                
                                data = header + "VDVBW,5.0,5.0,A,5.0,5.0,A";
                                CRC = xorIt(data);
                                AppendText(rtbConsole, "$" + data + "*" + CRC);
                                //if serial connected send the data
                                if (connected == true)
                                {
                                    port.Write("$" + data + "*" + CRC);
                                    port.Write(new byte[] { 13, 10 }, 0, 2);
                                }
                                Thread.Sleep(100);
                            }
                        }

                    }).Start();
                }

                else if (_type == "TEST")
                {
                    new Thread(() =>
                    {
                        Thread.CurrentThread.IsBackground = true;
                        while (true)
                        {
                            if (stop == true || endREVThread == true)
                            {
                                stop = false;
                                endFWDThread = false;
                                break;//exit loop
                            }
                            else
                            {
                                //Heading
                                data = "HEHDT,100.0,T";
                                CRC = xorIt(data);
                                AppendText(rtbConsole, "$" + data + "*" + CRC);
                                //if serial connected send the data
                                if (connected == true)
                                {
                                    port.Write("$" + data + "*" + CRC);
                                    port.Write(new byte[] { 13, 10 }, 0, 2);
                                }
                                Thread.Sleep(100);

                                //Speed
                                data = "VDVBW,5.0,5.0,A,5.0,5.0,A";
                                CRC = xorIt(data);
                                AppendText(rtbConsole, "$" + data + "*" + CRC);
                                //if serial connected send the data
                                if (connected == true)
                                {
                                    port.Write("$" + data + "*" + CRC);
                                    port.Write(new byte[] { 13, 10 }, 0, 2);
                                }
                                Thread.Sleep(100);
                            }
                        }

                    }).Start();
                }
                _isRunning = false;
                runningThreads--;//remove last thread
                setLabelText(lbThreads, runningThreads.ToString());
            }
        }

        public void AppendText(RichTextBox rtb, string value)
        {
            if (InvokeRequired)
            {
                try
                {
                    SetRichTextCallback d = new SetRichTextCallback(AppendText);
                    this.Invoke(d, new object[] { rtb, value });
                    //this.Invoke(new Action<string>(AppendTextBox), new object[] { value });
                }
                catch
                {

                }
                return;
            }
            else
            {
                rtb.AppendText(value + Environment.NewLine);
            }
        }

        public void setLabelText(Label rtb, string value)
        {
            if (InvokeRequired)
            {
                try
                {
                    SetLabelTextCallback d = new SetLabelTextCallback(setLabelText);
                    this.Invoke(d, new object[] { rtb, value });
                    //this.Invoke(new Action<string>(AppendTextBox), new object[] { value });
                }
                catch
                {

                }
                return;
            }
            else
            {
                lbThreads.Text = value;
            }
        }

        public static string xorIt(string _data)
        {
            int checksum = 0;
            for (int i = 0; i < _data.Length; i++)
            {
                checksum ^= Convert.ToByte(_data[i]);
            }

            return checksum.ToString("X2");
        }

        #endregion

        private void cbCont_CheckedChanged(object sender, EventArgs e)
        {
            tbEnd.Enabled = cbCont.Checked ? false : true;
        }

        private void cbNMEA_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (cbNMEA.SelectedItem.ToString() == "VBW")
                {
                    tbHeader.Clear();
                    tbHeader.Text = "VD";
                }
                else if (cbNMEA.SelectedItem.ToString() == "HDT")
                {
                    tbHeader.Clear();
                    tbHeader.Text = "HE";
                }
                else
                {
                    tbHeader.Clear();
                    tbHeader.Text = "NM";
                }
            }
            catch
            {

            }
        }
    }
}
