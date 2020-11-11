using System;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Windows.Forms;
using Modbus.Common;
using ModbusLib;
using ModbusLib.Protocols;
using System.IO;

namespace ModbusMaster
{
    public partial class MasterForm : BaseForm
    {
        private int _transactionId;
        private ModbusClient _driver;
        private ICommClient _portClient;
        private SerialPort _uart;

        private byte _lastReadCommand = 0;

        #region Form

        public MasterForm()
        {
            InitializeComponent();
            this.Text += String.Format(" ({0})", Assembly.GetExecutingAssembly().GetName().Version.ToString());
        }

        private void MasterFormClosing(object sender, FormClosingEventArgs e)
        {
            DoDisconnect();
        }

        #endregion

        #region Connect/disconnect

        private void DoDisconnect()
        {
            if (_socket != null)
            {
                _socket.Close();
                _socket.Dispose();
                _socket = null;
            }
            if (_uart != null)
            {
                _uart.Close();
                _uart.Dispose();
                _uart = null;
            }
            _portClient = null;
            _driver = null;
        }

        private void BtnConnectClick(object sender, EventArgs e)
        {
            try
            {
                switch (CommunicationMode)
                {
                    case CommunicationMode.RTU:
                        _uart = new SerialPort(PortName, Baud, Parity, DataBits, StopBits);
                        _uart.Open();
                        _portClient = _uart.GetClient();
                        _driver = new ModbusClient(new ModbusRtuCodec()) { Address = SlaveId };
                        _driver.OutgoingData += DriverOutgoingData;
                        _driver.IncommingData += DriverIncommingData;
                        AppendLog(String.Format("Connected using RTU to {0}", PortName));
                        break;

                    case CommunicationMode.UDP:
                        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                        _socket.Connect(new IPEndPoint(IPAddress, TCPPort));
                        _portClient = _socket.GetClient();
                        _driver = new ModbusClient(new ModbusTcpCodec()) { Address = SlaveId };
                        _driver.OutgoingData += DriverOutgoingData;
                        _driver.IncommingData += DriverIncommingData;
                        AppendLog(String.Format("Connected using UDP to {0}", _socket.RemoteEndPoint));
                        break;

                    case CommunicationMode.TCP:
                        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        _socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
                        _socket.SendTimeout = 2000;
                        _socket.ReceiveTimeout = 2000;
                        _socket.Connect(new IPEndPoint(IPAddress, TCPPort));
                        _portClient = _socket.GetClient();
                        _driver = new ModbusClient(new ModbusTcpCodec()) { Address = SlaveId };
                        _driver.OutgoingData += DriverOutgoingData;
                        _driver.IncommingData += DriverIncommingData;
                        AppendLog(String.Format("Connected using TCP to {0}", _socket.RemoteEndPoint));
                        break;
                }
            }
            catch (Exception ex)
            {
                AppendLog(ex.Message);
                return;
            }
            btnConnect.Enabled = false;
            buttonDisconnect.Enabled = true;
            groupBoxFunctions.Enabled = true;
            groupBoxTCP.Enabled = false;
            groupBoxRTU.Enabled = false;
            groupBoxMode.Enabled = false;
            grpExchange.Enabled = false;
        }

        private void ButtonDisconnectClick(object sender, EventArgs e)
        {
            DoDisconnect();
            btnConnect.Enabled = true;
            buttonDisconnect.Enabled = false;
            groupBoxFunctions.Enabled = false;
            groupBoxMode.Enabled = true;
            grpExchange.Enabled = true;
            SetMode();
            AppendLog("Disconnected");
        }

        #endregion

        #region Functions buttons

        private void BtnReadCoilsClick(object sender, EventArgs e)
        {
            ExecuteReadCommand(ModbusCommand.FuncReadCoils);

        }

        private void BtnReadDisInpClick(object sender, EventArgs e)
        {
            ExecuteReadCommand(ModbusCommand.FuncReadInputDiscretes);
        }

        private void BtnReadHoldRegClick(object sender, EventArgs e)
        {
            ExecuteReadCommand(ModbusCommand.FuncReadMultipleRegisters);
        }

        private void BtnReadInpRegClick(object sender, EventArgs e)
        {
            ExecuteReadCommand(ModbusCommand.FuncReadInputRegisters);
        }

        private void ExecuteReadCommand(byte function)
        {
            _lastReadCommand = function;

            try
            {
                var command = new ModbusCommand(function) {Offset = StartAddress, Count = DataLength, TransId = _transactionId++};
                var result = _driver.ExecuteGeneric(_portClient, command);
                if (result.Status == CommResponse.Ack)
                {
                    command.Data.CopyTo(_registerData, StartAddress);
                    UpdateDataTable();
                    AppendLog(String.Format("Read succeeded: Function code:{0}.", function));
                }
                else
                {
                    AppendLog(String.Format("Failed to execute Read: Error code:{0}", result.Status));
                }
            }
            catch (Exception ex)
            {
                AppendLog(ex.Message);
            }
        }


        private int ExecuteFileRecordWriteCommand(byte function, byte[] data, ushort fileNo, ushort recordNo)
        {
            try
            {
                DataLength = 64;

                ModbusCommand command = new ModbusCommand(function)
                {
                    Offset = StartAddress,
                    Count = DataLength,
                    TransId = _transactionId++,
                    Data = new ushort[DataLength],
                    FileData = data,
                    FileNo = fileNo,
                    RecordNo = recordNo
                };

                var result = _driver.ExecuteGeneric(_portClient, command);
                AppendLog(result.Status == CommResponse.Ack
                              ? String.Format("Write succeeded: Function code:{0}", function)
                              : String.Format("Failed to execute Write: Error code:{0}", result.Status));

                if (result.Status == CommResponse.Ack)
                {
                    //完成
                    return result.Status;
                }
            }
            catch (Exception ex)
            {
                AppendLog(ex.Message);
            }

            return CommResponse.Unknown;
        }

        private int ExecuteWriteCommandOpenFile(byte function, short subFunction, byte[] strFileName, long length)
        {
            try
            {
                ModbusCommand command = new ModbusCommand(function, subFunction)
                {
                    Offset = StartAddress,
                    Count = DataLength,
                    TransId = _transactionId++,
                    Data = new ushort[DataLength],
                    FileName = strFileName,
                    FileLength = length
                };

                var result = _driver.ExecuteGeneric(_portClient, command);
                AppendLog(result.Status == CommResponse.Ack
                              ? String.Format("Write succeeded: Function code:{0}", function)
                              : String.Format("Failed to execute Write: Error code:{0}", result.Status));

                if(result.Status == CommResponse.Ack)
                {
                    //请求确认，开始计算文件大小发送文件
                    //ExecuteWriteCommand(ModbusCommand.FuncFileRecordWrite, );
                    return result.Status;
                }
            }
            catch (Exception ex)
            {
                AppendLog(ex.Message);
            }

            return CommResponse.Unknown;

        }

        private void ExecuteWriteCommand(byte function)
        {
            try
            {
                var command = new ModbusCommand(function)
                                  {
                                      Offset = StartAddress,
                                      Count = DataLength,
                                      TransId = _transactionId++,
                                      Data = new ushort[DataLength]
                                  };
                for (int i = 0; i < DataLength; i++)
                {
                    var index = StartAddress + i;
                    if (index > _registerData.Length)
                    {
                        break;
                    }
                    command.Data[i] = _registerData[index];
                }
                var result = _driver.ExecuteGeneric(_portClient, command);
                AppendLog(result.Status == CommResponse.Ack
                              ? String.Format("Write succeeded: Function code:{0}", function)
                              : String.Format("Failed to execute Write: Error code:{0}", result.Status));
            }
            catch (Exception ex)
            {
                AppendLog(ex.Message);
            }
        }


        private void BtnWriteSingleCoilClick(object sender, EventArgs e)
        {
            try
            {
                var command = new ModbusCommand(ModbusCommand.FuncWriteCoil)
                {
                    Offset = StartAddress,
                    Count = 1,
                    TransId = _transactionId++,
                    Data = new ushort[1]
                };
                command.Data[0] = (ushort)(_registerData[StartAddress] & 0x0100);
                var result = _driver.ExecuteGeneric(_portClient, command);
                AppendLog(result.Status == CommResponse.Ack
                              ? String.Format("Write succeeded: Function code:{0}", ModbusCommand.FuncWriteCoil)
                              : String.Format("Failed to execute Write: Error code:{0}", result.Status));
            }
            catch (Exception ex)
            {
                AppendLog(ex.Message);
            }
        }

        private void BtnWriteSingleRegClick(object sender, EventArgs e)
        {
            //ExecuteWriteCommand(ModbusCommand.FuncWriteSingleRegister);
            //ExecuteWriteCommand(ModbusCommand.FuncReboot);
            //ExecuteWriteCommand(ModbusCommand.FuncSetTime);

            /*send file*/
            OpenFileDialog dialog = new OpenFileDialog();
            if(dialog.ShowDialog() == DialogResult.OK)
            {
                String fullPath = dialog.FileName;

                FileInfo f = new FileInfo(dialog.FileName);
                long length = f.Length;
                int point = fullPath.LastIndexOf('\\') + 1;
                string filePath = fullPath.Substring(0, point);
                string fileName = fullPath.Substring(point, fullPath.Length - point);
                byte[] byteArray = System.Text.Encoding.Default.GetBytes(fileName);

                FileStream fs = new FileStream(filePath + fileName, FileMode.Open);
                //  int 个数 = fs.Read(byteArray, 0, byteArray.Length);


                int result = ExecuteWriteCommandOpenFile(ModbusCommand.FuncFile, ModbusCommand.SubFuncFileWriteOpen, byteArray, length);
                if(result == CommResponse.Ack)
                {
                    //if (fs.Length <= 128)
                    //{
                    //    //一次就可以发送文件
                    //    byte[] fileByteArray = new byte[fs.Length];
                    //    int 个数 = fs.Read(fileByteArray, 0, fileByteArray.Length);
                    //    ushort fileNo = 0;
                    //    ushort recordNo = 0;
                    //    result = ExecuteFileRecordWriteCommand(ModbusCommand.FuncFileRecordWrite, fileByteArray, fileNo, recordNo);
                    //    if (result != CommResponse.Ack)
                    //    {
                    //        //错误
                    //    }
                    //}
                    //else if (fs.Length > 128 && fs.Length <= /*20000*/ 1000)
                    //{
                    //    //一个record file就可以发送文件
                    //    //一次就可以发送文件
                    //    byte[] fileByteArray = new byte[ModbusCommand.FileRecordDataLength];
                    //    int 个数 = fs.Read(fileByteArray, 0, fileByteArray.Length);
                    //    ushort fileNo = 0;
                    //    ushort recordNo = 0;
                    //    long statisticsLength = 0;
                    //    while (fileByteArray.Length == 个数)
                    //    {
                    //        result = ExecuteFileRecordWriteCommand(ModbusCommand.FuncFileRecordWrite, fileByteArray, fileNo, recordNo);
                    //        if (result != CommResponse.Ack)
                    //        {
                    //            //错误
                    //        }
                    //        statisticsLength += fileByteArray.Length;
                    //        fs.Seek(statisticsLength, SeekOrigin.Begin);
                    //        个数 = fs.Read(fileByteArray, 0, fileByteArray.Length);
                    //        recordNo++;
                    //    }

                    //    if(个数 > 0)
                    //    {

                    //        byte[] byteArrayTmp = new byte[个数];
                    //        Buffer.BlockCopy(fileByteArray, 0, byteArrayTmp, 0, 个数);
                    //        result = ExecuteFileRecordWriteCommand(ModbusCommand.FuncFileRecordWrite, byteArrayTmp, fileNo, recordNo);
                    //        if (result != CommResponse.Ack)
                    //        {
                    //            //错误
                    //        }
                    //        else
                    //        {
                    //            //写文件成功
                    //            //向下位机发送写文件完成命令
                    //            result = ExecuteWriteCommandOpenFile(ModbusCommand.FuncFile, ModbusCommand.SubFuncFileWriteDone, byteArray, length);
                    //            if(result != CommResponse.Ack)
                    //            {
                    //                //错误
                    //            }
                    //            else
                    //            {
                    //                AppendLog("写文件成功!");
                    //            }
                    //        }
                    //    }
                    //}
                    //if (fs.Length > /*20000*/ 128)
                    {
                        int statisticsLength = 0;
                        ushort fileNo = 0;
                        while (statisticsLength < fs.Length)
                        {
                            //一个record file就可以发送文件
                            //一次就可以发送文件
                            byte[] fileByteArray = new byte[ModbusCommand.FileRecordDataTransmitLength];
                            int 个数 = fs.Read(fileByteArray, 0, fileByteArray.Length);
                            fileNo++;
                            ushort recordNo = 0;
                            
                            int readLength = ModbusCommand.FileRecordDataTransmitLength;
                            while (fileByteArray.Length == 个数)
                            {
                                result = ExecuteFileRecordWriteCommand(ModbusCommand.FuncFileRecordWrite, fileByteArray, fileNo, recordNo);
                                if (result != CommResponse.Ack)
                                {
                                    //错误
                                }
                                statisticsLength += fileByteArray.Length;
                                if (statisticsLength + ModbusCommand.FileRecordDataTransmitLength > /*20000*/ (260 * fileNo))
                                {
                                    //fileNo++;
                                    //recordNo = 0;
                                    readLength = 260 * fileNo - statisticsLength;
                                }
                                fs.Seek(statisticsLength, SeekOrigin.Begin);
                                个数 = fs.Read(fileByteArray, 0, readLength);
                                recordNo++;
                            }

                            if (个数 > 0)
                            {

                                byte[] byteArrayTmp = new byte[个数];
                                Buffer.BlockCopy(fileByteArray, 0, byteArrayTmp, 0, 个数);
                                result = ExecuteFileRecordWriteCommand(ModbusCommand.FuncFileRecordWrite, byteArrayTmp, fileNo, recordNo);
                                if (result != CommResponse.Ack)
                                {
                                    //错误
                                }
                                else
                                {
                                    statisticsLength += 个数;

                                    
                                }
                            }
                        }

                        //写文件成功
                        //向下位机发送写文件完成命令
                        result = ExecuteWriteCommandOpenFile(ModbusCommand.FuncFile, ModbusCommand.SubFuncFileWriteDone, byteArray, length);
                        if (result != CommResponse.Ack)
                        {
                            //错误
                        }
                        else
                        {
                            AppendLog("写文件成功!");
                        }

                    }



                }


                fs.Dispose();
                fs.Close();

            }
        }



        private void BtnWriteMultipleCoilsClick(object sender, EventArgs e)
        {
            ExecuteWriteCommand(ModbusCommand.FuncForceMultipleCoils);
        }

        private void BtnWriteMultipleRegClick(object sender, EventArgs e)
        {
            ExecuteWriteCommand(ModbusCommand.FuncWriteMultipleRegisters);
        }

        private void ButtonReadExceptionStatusClick(object sender, EventArgs e)
        {

        }

        #endregion

        private void txtPollDelay_Leave(object sender, EventArgs e)
        {
            var textBox = (TextBox)sender;
            if (int.TryParse(textBox.Text, out var parsedMillisecs))
            {
                timer1.Interval = parsedMillisecs;
            }
            else
            {
                textBox.Text = "0";
                cbPoll.Checked = false;
                timer1.Enabled = false;
            }

        }

        private void cbPoll_CheckStateChanged(object sender, EventArgs e)
        {
            /*pollTimer.Enabled = cbPoll.Checked;

            if (!pollTimer.Enabled)
                _lastReadCommand = 0;
            */

            timer1.Enabled = cbPoll.Checked;

            if(!timer1.Enabled)
            {
                _lastReadCommand = 0;
            }
        }

        private void pollTimer_Tick(object sender, EventArgs e)
        {
            if (_lastReadCommand != 0)
                ExecuteReadCommand(_lastReadCommand);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (_lastReadCommand != 0)
                ExecuteReadCommand(_lastReadCommand);
        }

        private void MasterForm_Load(object sender, EventArgs e)
        {

        }

        private void buttonImport_Click(object sender, EventArgs e)
        {

        }

        private void cbPoll_CheckedChanged(object sender, EventArgs e)
        {
            timer1.Enabled = cbPoll.Checked;

            if (!timer1.Enabled)
            {
                _lastReadCommand = 0;
            }
        }


    }
}
