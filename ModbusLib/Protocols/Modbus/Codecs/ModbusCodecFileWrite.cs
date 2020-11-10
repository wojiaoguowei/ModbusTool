using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModbusLib.Protocols
{
    class ModbusCodecFileWrite
        : ModbusCommandCodec
    {
        #region Client codec

        public override void ClientEncode(
            ModbusCommand command,
            ByteArrayWriter body)
        {
            if (command.SubFunctionCode == ModbusCommand.SubFuncFileWriteOpen)
            {
                //file open read
                body.WriteInt16BE(command.SubFunctionCode);
                byte[] byteArray = command.FileName;
                int size = byteArray.Length;
                body.WriteBytes(byteArray);

                byte[] tmpArray = new byte[128 - size];
                body.WriteBytes(tmpArray);

                body.WriteInt64BE(command.FileLength);

            }
            else if (command.SubFunctionCode == ModbusCommand.SubFuncFileWriteDone)
            { 
                //file read done
            }
            
        }


        public override void ClientDecode(
            ModbusCommand command,
            ByteArrayReader body)
        {
            //not used
            if(command.FunctionCode != ModbusCommand.FuncFile)
            {
                byte exceptionCodeFunc = body.ReadByte();
                if (exceptionCodeFunc == ModbusCommand.FuncExecptionCodeFileOpened)
                {
                    //文件已被打开
                }
                else if (exceptionCodeFunc == ModbusCommand.FuncExecptionCodeFileunfinished)
                {
                    //有未完成的文件操作
                }
            }
            

        }

        #endregion
    }
}
